"""End-to-end check on an isolated Compose project; requires Docker and Python 3.9+."""

import csv
import json
import os
from pathlib import Path
import subprocess
import tempfile
import urllib.error
import urllib.parse
import urllib.request
import uuid


ROOT = Path(__file__).resolve().parent.parent


def main():
    artifacts = ROOT / "artifacts"
    artifacts.mkdir(exist_ok=True)
    with tempfile.TemporaryDirectory(prefix="vector-test-", dir=artifacts) as directory:
        directory = Path(directory)
        final_stanza = "Коралловые рифы тропического океана, разноцветные рыбы и морские черепахи."
        poems = [
            ("Море", "Ночная луна", "La lune éclaire la mer. Les vagues brillent dans la nuit."),
            ("Пекарь", "Пирог", "Смешайте муку, яйца и сахар. Поставьте пирог в печь."),
            ("Пассажир", "Вокзал", "Поезд прибыл на вокзал. Пассажиры покупают билеты."),
            ("Путешественник", "Длинное стихотворение",
             "\n\n".join(f"Зимняя строфа {i}.\nЗаснеженный лес и холодный ветер, лёд и метель."
                         for i in range(100)) + "\n\n" + final_stanza),
        ]
        with (directory / "russianPoetryWithTheme.csv").open("w", encoding="utf-8", newline="") as file:
            writer = csv.writer(file)
            writer.writerow(["author", "name", "text"])
            writer.writerows(poems)

        override = directory / "compose.test.yaml"
        override.write_text("""services:
  embeddings:
    image: poems-embeddings:vector-test
    pull_policy: build
  poems.loader:
    image: poems-loader:vector-test
  poems.site:
    image: poems-site:vector-test
""", encoding="utf-8")
        environment = dict(os.environ, COMPOSE_PROFILES="", VECTOR_SEARCH_ENABLED="true",
                           ELASTICSEARCH_ENABLED="false", FOLDER_WITH_POEMS=str(directory), SITE_PORT="0",
                           EMBEDDINGS_CPUS="2", EMBEDDINGS_THREADS="2", EMBEDDINGS_BATCH_SIZE="2",
                           EMBEDDINGS_MEM_LIMIT="1g")
        command = ["docker", "compose", "-p", "poems-vector-test-" + uuid.uuid4().hex[:8],
                   "-f", str(ROOT / "compose/compose.yaml"),
                   "-f", str(ROOT / "compose/compose.local.yaml"), "-f", str(override)]
        profiles = ["--profile", "vector"]

        def compose(*arguments, capture=False):
            result = subprocess.run(command + profiles + list(arguments), cwd=ROOT / "compose", env=environment,
                                    check=True, capture_output=capture, text=True, encoding="utf-8")
            return result.stdout.strip() if capture else ""

        def request(path, status=200):
            try:
                with urllib.request.urlopen(base_url + path, timeout=40) as response:
                    body = response.read().decode("utf-8")
                    assert response.status == status, (response.status, body)
                    return body
            except urllib.error.HTTPError as error:
                body = error.read().decode("utf-8")
                assert error.code == status, (error.code, body)
                return body

        def search(query, engine="vector", status=200):
            return request("/api/poems/search/" + engine + "?" + urllib.parse.urlencode({"query": query}), status)

        def counts():
            return compose("exec", "-T", "postgres", "psql", "-U", "poemUser", "-d", "poems", "-Atc",
                           "SELECT count(*), count(DISTINCT poem_id), min(vector_dims(embedding)) FROM poem_chunks",
                           capture=True)

        try:
            compose("up", "-d", "--build", "--wait", "--wait-timeout", "300")
            base_url = "http://" + compose("port", "poems.site", "8080", capture=True)
            assert 'data-vector-search-enabled="true"' in request("/")
            result = json.loads(search("ночное море и лунный свет"))
            assert result["poems"][0]["title"] == "Ночная луна", result
            ids = [poem["id"] for poem in result["poems"]]
            assert len(ids) == len(set(ids)) == 4, ids
            result = json.loads(search("коралловые рифы тропического океана"))
            assert result["poems"][0]["title"] == "Длинное стихотворение", result
            poem = json.loads(request("/api/poems/" + str(result["poems"][0]["id"])))
            assert poem["content"].endswith(final_stanza)
            assert "256" in search("я " * 300, status=400)
            search("а" * 1001, status=400)
            search("я", status=400)
            assert json.loads(search("Пирог", "db"))["poems"]

            before = counts()
            chunks, documents, dimensions = map(int, before.split("|"))
            assert chunks > documents == 4 and dimensions == 384, before
            compose("run", "--rm", "--no-deps", "poems.loader")
            assert counts() == before, "Repeated loader run duplicated chunks"

            compose("stop", "embeddings")
            search("морские волны", status=503)
            assert json.loads(search("Пирог", "db"))["poems"]

            environment["VECTOR_SEARCH_ENABLED"] = "false"
            profiles.clear()
            assert "embeddings" not in compose("config", "--services", capture=True).splitlines()
            compose("up", "-d", "--no-deps", "--wait", "poems.site")
            base_url = "http://" + compose("port", "poems.site", "8080", capture=True)
            assert 'data-vector-search-enabled="false"' in request("/")
            assert "отключён" in search("морские волны", status=503)
            assert json.loads(search("Пирог", "db"))["poems"]
            print("PASS: multilingual retrieval, long-poem tail, deduplication, limits, repeat import, optional mode")
        finally:
            compose("--profile", "*", "down", "--volumes", "--remove-orphans")


if __name__ == "__main__":
    main()
