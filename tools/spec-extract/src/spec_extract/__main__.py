# SPDX-FileCopyrightText: 2026 Starion Group S.A.
# SPDX-License-Identifier: Apache-2.0

"""Thin, non-interactive entry point so the `reqifsharp-sage` C# CLI can invoke this pipeline.

    python -m spec_extract extract --pdf <path-to-pdf> --out <output-dir> [--document ReqIF] [--version 1.2]

The primary dev workflow for this package is still pytest (see tests/), matching
mycelium-hypha's "driven by tests, not a distributed CLI" approach; this entry point
exists only for the `generate` verb's `spec` step to shell out to non-interactively.

`--document` selects the document profile (see profiles.py): "ReqIF" for the OMG ReqIF formal
specification, "PSI-Guide" for the prostep ivip ReqIF Implementation Guide.
"""

from __future__ import annotations

import argparse
import sys

from spec_extract.pipeline import extract_document


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(prog="spec_extract")
    subparsers = parser.add_subparsers(dest="command", required=True)

    extract_parser = subparsers.add_parser("extract", help="Extract clause markdown from a spec PDF.")
    extract_parser.add_argument("--pdf", required=True, help="Path to the source PDF.")
    extract_parser.add_argument("--out", required=True, help="Output directory for clauses/, index.json, index.md.")
    extract_parser.add_argument(
        "--document",
        default="ReqIF",
        help="Document name (ReqIF, PSI-Guide) recorded in front matter; selects the profile.",
    )
    extract_parser.add_argument("--version", default="1.2", help="Spec version recorded in front matter.")

    args = parser.parse_args(argv)

    if args.command == "extract":
        result = extract_document(args.pdf, args.out, document=args.document, version=args.version)
        print(f"Extracted {len(result.clauses)} clauses to {result.output_dir}")
        if result.missing_toc_entries:
            print(f"{len(result.missing_toc_entries)} Table of Contents entries had no heading", file=sys.stderr)
        return 0

    return 1


if __name__ == "__main__":
    sys.exit(main())
