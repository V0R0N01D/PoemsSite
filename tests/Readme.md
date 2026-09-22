
### Проверки

Из корня репозитория:

```bash
dotnet build Poems.sln --configuration Release
python tests/vector_smoke.py
```

Smoke-тест собирает образы и запускает отдельный Compose-проект на случайном локальном порту
с небольшим тестовым CSV. Проверяет русско-французский поиск, поиск по концу длинного стихотворения,
отсутствие дублей, оба ограничения ввода, повторный запуск загрузчика, отказ модели и отключение режима.
В конце удаляет только свои контейнеры и volumes. При первом запуске нужен доступ к реестрам образов
и Hugging Face. Для отдельных проверок модели и фрагментов после сборки образа, из папки `compose`:

```bash
docker compose -f compose.yaml -f compose.local.yaml --profile vector run --rm --no-deps embeddings python -m unittest -v test_embeddings
```