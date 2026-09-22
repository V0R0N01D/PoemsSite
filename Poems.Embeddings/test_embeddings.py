"""Run inside the embeddings image with the model cache mounted (see README)."""

import re
import unittest

import numpy as np

from embeddings import (
    DIMENSIONS, MAX_TOKENS, EmbeddingEngine, chunk_poem, document_prefix,
)


class EmbeddingTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.engine = EmbeddingEngine()
        cls.tokenizer = cls.engine.tokenizer

    def test_long_mixed_language_poem_keeps_all_words_and_fits_context(self):
        text = "\n\n".join(
            f"Строфа{number}: я помню чудное мгновенье,\n"
            f"Écoute{number}, mon cœur chante sous la lune."
            for number in range(100)
        ) + "\nПоследняяСтрока: adieu, mon amour!"
        prefix = document_prefix(self.tokenizer, "Автор " * 100, "Название " * 100)
        chunks = chunk_poem(self.tokenizer, text, prefix)
        self.assertGreater(len(chunks), 1)
        for chunk in chunks:
            self.assertLessEqual(len(self.tokenizer.encode(prefix + chunk).ids), MAX_TOKENS)
        words = set(re.findall(r"\w+", text))
        self.assertTrue(words.issubset(set(re.findall(r"\w+", "\n".join(chunks)))))
        self.assertTrue(chunks[-1].endswith("adieu, mon amour!"))
        self.assertTrue(any(chunks[0].endswith(chunks[1][:size]) for size in range(10, 500)))

    def test_single_very_long_line_is_not_truncated(self):
        text = " ".join(f"слово{number}" for number in range(2000))
        chunks = chunk_poem(self.tokenizer, text, "passage: ")
        self.assertGreater(len(chunks), 1)
        self.assertTrue(chunks[-1].endswith("слово1999"))
        self.assertTrue(all(len(self.tokenizer.encode("passage: " + chunk).ids) <= MAX_TOKENS
                            for chunk in chunks))

    def test_short_poem_and_unicode(self):
        text = "Я вас любил.\nJe t’aime, ô mon cœur."
        self.assertEqual([text], chunk_poem(self.tokenizer, text, "passage: "))

    def test_token_limit_is_checked_independently_of_character_limit(self):
        query = "я " * 300
        self.assertLess(len(query), 1000)
        with self.assertRaisesRegex(ValueError, "256 токенов"):
            self.engine.embed_query(query)
        for invalid in ("я", "а" * 1001):
            with self.assertRaises(ValueError):
                self.engine.embed_query(invalid)

    def test_real_embeddings_are_normalized_and_find_french_passage(self):
        query = np.array(self.engine.embed_query("ночное море и лунный свет"))
        documents = [
            "passage: La lune éclaire la mer. Les vagues brillent dans la nuit.",
            "passage: Рецепт пирога: смешайте муку, яйца и сахар, поставьте в печь.",
            "passage: Поезд прибыл на вокзал. Пассажиры покупают билеты.",
        ]
        vectors = np.array(self.engine.embed(documents))
        self.assertEqual((3, DIMENSIONS), vectors.shape)
        self.assertTrue(np.isfinite(vectors).all())
        np.testing.assert_allclose(np.linalg.norm(vectors, axis=1), 1, atol=1e-5)
        self.assertEqual(0, int(np.argmax(vectors @ query)))


if __name__ == "__main__":
    unittest.main()
