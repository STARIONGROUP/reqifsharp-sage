# SPDX-FileCopyrightText: 2026 Starion Group S.A.
# SPDX-License-Identifier: Apache-2.0

"""Stage 6: orchestrate stages 1-5 across a whole PDF and write the knowledge-base output.

Output layout (matches the reqifsharp-sage knowledge base convention):
    <output_dir>/clauses/<number>-<slug>.md   one file per clause
    <output_dir>/index.json                   array of {clause, title, document, pages, normative, file,
                                              license, redistributable}, sorted by clause number
    <output_dir>/index.md                     human-readable table of the same

The `--document` name selects a `DocumentProfile` (see profiles.py) for the formatting details and
licence that differ between documents.

This module is deliberately not a distributed CLI in the usual sense - see __main__.py
for the thin entry point that exists only so the C# `generate` verb can invoke it.
"""

from __future__ import annotations

import json
import statistics
import sys
from dataclasses import dataclass, field
from pathlib import Path

from spec_extract.clauses import detect_clauses_and_toc
from spec_extract.layout import reconstruct_pages
from spec_extract.markdown import render_clause_markdown
from spec_extract.models import Clause, ReconstructedPage, clause_number_key
from spec_extract.normative import document_line_spacing, split_normative
from spec_extract.pdf_reader import read_positioned_pages
from spec_extract.profiles import DocumentProfile, profile_for
from spec_extract.toc import TocEntry


@dataclass
class ExtractionResult:
    """What one run of the pipeline produced, for callers/tests to inspect."""

    clauses: list[Clause]
    output_dir: Path
    profile: DocumentProfile
    toc_entries: list[TocEntry] = field(default_factory=list)

    @property
    def missing_toc_entries(self) -> list[TocEntry]:
        """Table of Contents entries no clause heading was found for."""
        found = {clause.number for clause in self.clauses}
        return [entry for entry in self.toc_entries if entry.number not in found]


def _write_text(path: Path, content: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="\n") as handle:
        handle.write(content)


def _pages(clause: Clause) -> str:
    return f"{clause.page_start}" if clause.page_start == clause.page_end else f"{clause.page_start}-{clause.page_end}"


def _write_index(output_dir: Path, clauses: list[Clause], *, document: str, profile: DocumentProfile) -> None:
    # `document` is included on every row (not just in each clause's own front matter) so that two
    # sibling knowledge trees extracted from different documents (e.g. the ReqIF specification and the
    # implementation guide) never produce byte-shape-identical rows an agent could ground in the wrong one.
    rows = [
        {
            "clause": clause.number,
            "title": clause.title,
            "document": document,
            "pages": _pages(clause),
            "normative": clause.is_normative,
            "file": f"clauses/{clause.file_name}",
            "license": profile.license,
            "redistributable": profile.redistributable,
        }
        for clause in clauses
    ]
    _write_text(output_dir / "index.json", json.dumps(rows, indent=2, sort_keys=True) + "\n")

    header = ["Clause", "Title", "Document", "Pages", "Normative", "File", "License", "Redistributable"]
    lines = ["| " + " | ".join(header) + " |", "|" + "---|" * len(header)]
    for row in rows:
        cells = [row[name.lower()] for name in header]
        lines.append("| " + " | ".join(str(cell).replace("|", "\\|") for cell in cells) + " |")
    _write_text(output_dir / "index.md", "\n".join(lines) + "\n")


def _body_font_size(pages: list[ReconstructedPage]) -> float:
    sizes = [word.size for page in pages for line in page.lines for word in line.words if word.size > 0]
    return statistics.mode(sizes) if sizes else 0.0


def _line_spacing(pages: list[ReconstructedPage]) -> float:
    return document_line_spacing([list(page.lines) for page in pages], _body_font_size(pages))


def extract_document(
    pdf_path: str | Path, output_dir: str | Path, *, document: str = "ReqIF", version: str = "1.2"
) -> ExtractionResult:
    """Run the full pipeline against `pdf_path`, writing clause markdown + indexes into `output_dir`."""
    output_dir = Path(output_dir)
    profile = profile_for(document)

    positioned_pages = read_positioned_pages(pdf_path)
    reconstructed_pages = reconstruct_pages(positioned_pages, strip_patterns=profile.strip_patterns)
    clauses, toc_entries = detect_clauses_and_toc(reconstructed_pages, toc_pages=profile.toc_pages)
    line_spacing = _line_spacing(reconstructed_pages)
    for clause in clauses:
        split_normative(
            clause,
            line_spacing=line_spacing or None,
            normative_pattern=profile.normative_pattern,
            recommendation_pattern=profile.recommendation_pattern,
        )

    clauses = sorted(clauses, key=lambda clause: clause_number_key(clause.number))

    # A re-run must not leave a previous run's clause files behind (e.g. after a title fix renamed one).
    for stale in (output_dir / "clauses").glob("*.md"):
        stale.unlink()
    for clause in clauses:
        markdown = render_clause_markdown(
            clause,
            document=document,
            version=version,
            license=profile.license,
            redistributable=profile.redistributable,
        )
        _write_text(output_dir / "clauses" / clause.file_name, markdown)

    _write_index(output_dir, clauses, document=document, profile=profile)

    result = ExtractionResult(clauses=clauses, output_dir=output_dir, profile=profile, toc_entries=toc_entries)
    for entry in result.missing_toc_entries:
        print(f"warning: no heading found for Table of Contents entry {entry.number} {entry.title}", file=sys.stderr)
    return result
