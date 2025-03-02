# PoemsSite

Одностраничное приложение, реализующее автопоиск при наборе текста с использованием Entity Framework для полнотекстового
поиска.

## Описание

Данное приложение представляет собой одностраничный сайт на ASP.NET Core с одним текстовым полем, реализующий механизм
автопоиска при наборе текста. Поиск осуществляется посредством полнотекстового поиска в базе данных через Entity
Framework, а результаты отображаются в виде выпадающего списка.

## Технологии

- ASP.NET Core
- Entity Framework Core
- PostgreSQL
- Docker

## Доступный экземпляр

Вы можете ознакомиться с работающим экземпляром проекта на [сервере](https://v0r0n.ru/).

## Запуск приложения с использованием Docker Compose

### Предварительные требования
- Docker и Docker Compose

### Пошаговая инструкция

1. **Скачайте набор данных.**  
   Загрузите файл `russianPoetryWithTheme.csv` с [Russian Poetry Dataset](https://www.kaggle.com/datasets/greencools/russianpoetry) и распакуйте архив.  
   Либо создайте файл `download.py` со следующим содержимым и выполните его в целевой директории:

   ```python
   import urllib.request
   import zipfile

   url = 'https://www.kaggle.com/api/v1/datasets/download/greencools/russianpoetry'
   zip_filename = 'russianpoetry.zip'

   urllib.request.urlretrieve(url, zip_filename)

   with zipfile.ZipFile(zip_filename, 'r') as archive:
       archive.extractall()
   ```

2. **Получите исходный код.**  
   Скачайте папку `compose` из корня репозитория или клонируйте весь репозиторий:

   ```bash
   git clone https://github.com/V0R0N01D/PoemsSite.git
   cd ./PoemsSite/compose
   ```

3. **Настройте окружение.**  
   В папке `compose` отредактируйте файл `.env`, указав в параметре `FolderWithPoems` путь к директории, где находится загруженный файл `russianPoetryWithTheme.csv`.


4. **Запустите приложение.**  
   Выполните следующую команду для запуска контейнеров:

   ```bash
   docker compose up -d
   ```

5. **Доступ к приложению.**  
   После запуска приложение будет доступно по адресу: [http://localhost:32164](http://localhost:32164)