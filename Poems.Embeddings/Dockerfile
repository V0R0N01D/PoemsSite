FROM python:3.12-slim
WORKDIR /app
ENV PYTHONUNBUFFERED=1 \
    PYTHONDONTWRITEBYTECODE=1 \
    TOKENIZERS_PARALLELISM=false \
    HF_HOME=/models \
    HF_HUB_DISABLE_XET=1
COPY Poems.Embeddings/requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt
COPY Poems.Embeddings/*.py .
EXPOSE 8000
CMD ["uvicorn", "app:app", "--host", "0.0.0.0", "--port", "8000", "--workers", "1", "--limit-concurrency", "16", "--timeout-keep-alive", "5"]
