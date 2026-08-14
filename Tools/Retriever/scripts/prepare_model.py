from __future__ import annotations

import argparse
from pathlib import Path

from sentence_transformers import SentenceTransformer


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--model", default="BAAI/bge-small-zh-v1.5")
    parser.add_argument(
        "--output", default=".build-assets/models/bge-small-zh-v1.5"
    )
    arguments = parser.parse_args()
    output = Path(arguments.output).resolve()
    output.parent.mkdir(parents=True, exist_ok=True)
    model = SentenceTransformer(arguments.model, device="cpu")
    model.save_pretrained(str(output))
    print(output)


if __name__ == "__main__":
    main()

