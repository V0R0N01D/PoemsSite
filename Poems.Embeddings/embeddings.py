"""CPU-only E5 inference. Changes to the model/chunking require a fresh index."""

import os
import re
from bisect import bisect_left, bisect_right
from functools import lru_cache

import numpy as np
import onnxruntime as ort
from huggingface_hub import hf_hub_download
from tokenizers import Tokenizer

MODEL = "intfloat/multilingual-e5-small"
REVISION = "614241f622f53c4eeff9890bdc4f31cfecc418b3"
MODEL_FILE = "onnx/model_qint8_avx512_vnni.onnx"
# Also checked by Poems.Common. Pin the tokenizer, weights and preprocessing together.
VERSION = "multilingual-e5-small-int8-v1"
DIMENSIONS = 384
MAX_TOKENS = 512
QUERY_TOKENS = 256
QUERY_CHARACTERS = 1000
CHUNK_TOKENS = 400
OVERLAP_TOKENS = 48


def document_prefix(tokenizer: Tokenizer, author: str, title: str) -> str:
    metadata = f"{author.strip()}\n{title.strip()}"
    offsets = tokenizer.encode(metadata, add_special_tokens=False).offsets
    if len(offsets) > 64:
        metadata = metadata[:offsets[63][1]]
    return f"passage: {metadata}\n"


def chunk_poem(tokenizer: Tokenizer, content: str, prefix: str) -> list[str]:
    """Prefer stanza/line boundaries; slice original Unicode text, never decoded tokens."""
    text = content.replace("\r\n", "\n").replace("\r", "\n").strip()
    if not text:
        return [""]
    offsets = tokenizer.encode(text, add_special_tokens=False).offsets
    starts = [start for start, _ in offsets]
    ends = [end for _, end in offsets]
    stanza_ends = [match.end() for match in re.finditer(r"\n[ \t]*\n", text)]
    line_ends = [match.end() for match in re.finditer(r"\n", text)]
    chunks = []
    start = 0
    while start < len(text):
        first = bisect_right(ends, start)
        stop = min(first + CHUNK_TOKENS, len(offsets))
        end = len(text) if stop == len(offsets) else starts[stop]
        if stop < len(offsets):
            # Avoid tiny chunks when a stanza is longer than the target window.
            minimum = ends[min(first + CHUNK_TOKENS // 2, stop) - 1]
            for boundaries in (stanza_ends, line_ends):
                index = bisect_right(boundaries, end) - 1
                if index >= 0 and boundaries[index] >= minimum:
                    end = boundaries[index]
                    break

        # Tokenization at a fragment boundary can differ from whole-document tokenization.
        while len(tokenizer.encode(prefix + text[start:end].strip()).ids) > MAX_TOKENS:
            stop = bisect_left(starts, end) - 1
            if stop <= first:
                raise ValueError("A fragment cannot fit into the model context")
            end = starts[stop]

        if end <= start:
            raise ValueError("Chunking made no progress")
        chunks.append(text[start:end].strip())
        if end == len(text):
            break

        last = bisect_left(starts, end)
        next_start = starts[max(first + 1, last - OVERLAP_TOKENS)]
        # Keep complete overlapping lines when a boundary lies around 40–60 tokens back.
        low = starts[max(first + 1, last - 60)]
        high = starts[max(first + 1, last - 40)]
        line = bisect_left(line_ends, low)
        if line < len(line_ends) and line_ends[line] <= high:
            next_start = line_ends[line]
        start = next_start if start < next_start < end else end
    return chunks


class EmbeddingEngine:
    def __init__(self):
        self.tokenizer = Tokenizer.from_file(hf_hub_download(
            MODEL, "tokenizer.json", revision=REVISION))
        # Never silently truncate poems or queries.
        self.tokenizer.no_truncation()
        self.tokenizer.no_padding()
        options = ort.SessionOptions()
        options.intra_op_num_threads = max(1, int(os.getenv("EMBEDDINGS_THREADS", "2")))
        options.inter_op_num_threads = 1
        self.session = ort.InferenceSession(
            hf_hub_download(MODEL, MODEL_FILE, revision=REVISION),
            sess_options=options, providers=["CPUExecutionProvider"])
        self.batch_size = max(1, min(8, int(os.getenv("EMBEDDINGS_BATCH_SIZE", "2"))))
        self.input_names = {value.name for value in self.session.get_inputs()}
        self.pad_id = self.tokenizer.token_to_id("<pad>")

    def embed(self, texts: list[str]) -> list[list[float]]:
        vectors = []
        for start in range(0, len(texts), self.batch_size):
            encoded = self.tokenizer.encode_batch(texts[start:start + self.batch_size])
            length = max(len(value.ids) for value in encoded)
            if length > MAX_TOKENS:
                raise ValueError("Input exceeds the model context")
            ids = np.full((len(encoded), length), self.pad_id, dtype=np.int64)
            mask = np.zeros_like(ids)
            for row, value in enumerate(encoded):
                ids[row, :len(value.ids)] = value.ids
                mask[row, :len(value.ids)] = 1
            inputs = {"input_ids": ids, "attention_mask": mask,
                      "token_type_ids": np.zeros_like(ids)}
            hidden = self.session.run(None, {
                key: value for key, value in inputs.items() if key in self.input_names
            })[0]
            # E5 uses attention-mask-aware mean pooling followed by L2 normalization.
            weights = mask[:, :, None].astype(np.float32)
            pooled = (hidden * weights).sum(axis=1) / weights.sum(axis=1)
            norms = np.linalg.norm(pooled, axis=1, keepdims=True)
            if pooled.shape[1] != DIMENSIONS or not np.isfinite(pooled).all() or (norms == 0).any():
                raise ValueError("Invalid model output")
            vectors.extend((pooled / norms).tolist())
        return vectors

    @lru_cache(maxsize=128)
    def embed_query(self, query: str) -> list[float]:
        # Count UTF-16 units, matching HTML maxlength and .NET string.Length.
        if not 3 <= len(query.encode("utf-16-le")) // 2 <= QUERY_CHARACTERS:
            raise ValueError("Введите от 3 до 1000 символов для поиска по смыслу.")
        text = "query: " + query
        if len(self.tokenizer.encode(text).ids) > QUERY_TOKENS:
            raise ValueError("Сократите поисковый запрос: максимум 256 токенов.")
        return self.embed([text])[0]

    def embed_poem(self, author: str, title: str, content: str) -> list[dict]:
        prefix = document_prefix(self.tokenizer, author, title)
        chunks = chunk_poem(self.tokenizer, content, prefix)
        vectors = self.embed([prefix + chunk for chunk in chunks])
        return [{"text": text, "embedding": vector} for text, vector in zip(chunks, vectors)]
