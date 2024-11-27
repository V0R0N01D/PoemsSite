# PoemsSite

Одностраничное приложение, реализующее автопоиск при наборе текста с использованием Entity Framework для полнотекстового поиска.

## Описание

Данное приложение представляет собой одностраничный сайт на ASP.NET Core с одним текстовым полем, реализующий механизм автопоиска при наборе текста. Поиск осуществляется посредством полнотекстового поиска в базе данных через Entity Framework, а результаты отображаются в виде выпадающего списка.

## Технологии

- ASP.NET Core
- Entity Framework Core
- PostgreSQL
- Docker
- Полнотекстовый поиск (Full-text search)

## Запущенный экземпляр

Экземпляр данного приложения запущен на [сервере](http://93.127.222.164:83/).
- Экземпляр подключен к базе данных с данными из набора данных: [Russian Poetry Dataset](https://www.kaggle.com/datasets/greencools/russianpoetry).

## Запуск приложения

### 1. Подготовка базы данных
- Установить PostgreSQL
- Создать новую базу данных
- Выполнить скрипт `db_code.txt` в созданной базе данных

### 2. Загрузка данных
- Скачать набор данных в формате CSV с [Russian Poetry Dataset](https://www.kaggle.com/datasets/greencools/russianpoetry)
- В проекте Poems.Loader:
  - Переименовать `example_connection_strings.json` в `connection_strings.json`
  - Обновить строку подключения к базе данных в `connection_strings.json`
  - Указать путь к CSV файлу в `appsettings.json` (параметр PathToFile)
  - Запустить Loader для импорта данных

### 3. Настройка веб-приложения
- В проекте Poems.Site:
  - Переименовать `example_connection_strings.json` в `connection_strings.json`
  - Обновить строку подключения к базе данных в `connection_strings.json`

### 4. Развертывание в Docker
- Создать образ приложения выполнив команду в корневой директории ./Poems
```bash
docker build -t poems-site -f Dockerfile .
```

- Запустить контейнер командой
```bash
docker run -d --name site --rm -p 80:8080 poems-site
```

