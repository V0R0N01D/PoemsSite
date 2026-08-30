"""Download the poetry dataset using only the Python standard library."""

import argparse
from pathlib import Path
import shutil
import sys
import tempfile
import urllib.error
import urllib.request
import zipfile


DATASET_URL = "https://www.kaggle.com/api/v1/datasets/download/greencools/russianpoetry"
CSV_NAME = "russianPoetryWithTheme.csv"
DATA_DIRECTORY = Path(__file__).resolve().parent / "init-data"


def download(force=False):
    DATA_DIRECTORY.mkdir(parents=True, exist_ok=True)
    destination = DATA_DIRECTORY / CSV_NAME
    if destination.is_file() and not force:
        print(f"Already downloaded: {destination} (use --force to download again)")
        return

    # Keep temporary files on the same filesystem for an atomic replacement.
    with tempfile.TemporaryDirectory(dir=DATA_DIRECTORY) as temporary_directory:
        temporary_path = Path(temporary_directory)
        archive_path = temporary_path / "russianpoetry.zip"
        print("Downloading Russian Poetry Dataset...", flush=True)
        request = urllib.request.Request(DATASET_URL, headers={"User-Agent": "PoemsSite/1.0"})
        with urllib.request.urlopen(request, timeout=60) as response:
            with archive_path.open("wb") as output:
                shutil.copyfileobj(response, output)

        with zipfile.ZipFile(archive_path) as archive:
            matches = [member for member in archive.infolist()
                       if not member.is_dir() and Path(member.filename).name == CSV_NAME]
            if len(matches) != 1:
                raise ValueError(f"Expected exactly one {CSV_NAME} in the downloaded archive")
            extracted_path = temporary_path / CSV_NAME
            # Extract only the required CSV; never trust archive paths.
            with archive.open(matches[0]) as source, extracted_path.open("wb") as output:
                shutil.copyfileobj(source, output)
            if extracted_path.stat().st_size == 0:
                raise ValueError("The downloaded CSV is empty")
            extracted_path.replace(destination)

    print(f"Saved: {destination}")


def main():
    parser = argparse.ArgumentParser(description=f"Download {CSV_NAME} into compose/init-data.")
    parser.add_argument("--force", action="store_true", help="Replace an existing dataset")
    args = parser.parse_args()
    try:
        download(force=args.force)
    except (OSError, urllib.error.URLError, zipfile.BadZipFile, ValueError) as error:
        print(f"Download failed: {error}", file=sys.stderr)
        return 1
    except KeyboardInterrupt:
        print("\nDownload cancelled.", file=sys.stderr)
        return 130
    return 0


if __name__ == "__main__":
    sys.exit(main())
