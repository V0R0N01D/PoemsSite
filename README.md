# PoemsSite

Одностраничное приложение, реализующее автопоиск при наборе текста с использованием PostgreSQL или Elasticsearch для
полнотекстового
поиска.

## Описание

Данное приложение представляет собой одностраничный сайт на ASP.NET Core с одним текстовым полем, реализующий механизм
автопоиска при наборе текста. Поиск осуществляется посредством полнотекстового поиска в базе данных Postgres через
Entity
Framework или при помощи Elasticsearch, а результаты отображаются в виде выпадающего списка.

## Технологии

- ASP.NET Core
- Entity Framework Core
- PostgreSQL
- Docker
- Elasticsearch

## Доступный экземпляр

Вы можете ознакомиться с работающим экземпляром проекта на [сервере](https://v0r0n.ru/).

## Запуск приложения с использованием Docker Compose

### Предварительные требования

- Docker и Docker Compose
- Python 3.9+ для скачивания набора данных

### Пошаговая инструкция

1. **Получите исходный код.**
   Скачайте папку `compose` из корня репозитория или клонируйте весь репозиторий:
   ```bash
   git clone https://github.com/V0R0N01D/PoemsSite.git
   cd ./PoemsSite/compose
   ```

2. **Скачайте набор данных одной командой.**
   ```bash
   python download.py
   ```
   На Linux/macOS при необходимости используйте `python3` вместо `python`.
   Скрипт скачивает [Russian Poetry Dataset](https://www.kaggle.com/datasets/greencools/russianpoetry)
   и сохраняет только `russianPoetryWithTheme.csv` в `compose/init-data`.

3. **Запустите приложение.**
    - Для запуска базовой версии (без Elasticsearch):
      ```bash
      docker compose up -d
      ```
    - Для запуска версии с Elasticsearch (требует больше оперативной памяти):
        1. Измените параметр `ELASTICSEARCH_ENABLED` на `true` в файле `.env`
        2. Выполните команду:
           ```bash
           docker compose --profile elastic up -d
           ```
    - Для остановки:
      ```bash
      docker compose --profile elastic down
      ```
    - Для остановки с удалением данных:
      ```bash
      docker compose --profile elastic down -v
      ```

4. **Доступ к приложению.**  
   После запуска приложение будет доступно по адресу: [http://localhost:32164](http://localhost:32164)
