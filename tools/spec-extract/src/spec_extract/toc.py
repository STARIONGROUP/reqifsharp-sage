"""Find and parse the Table of Contents.

The Table of Contents is located either from a document profile's page range or by its structure:
the page titled "Contents"/"Table of Contents" and the pages after it that stay dense with
dotted-leader entries ("... title ..... NN" - a TOC's structural signature, robust to however many
pages it spans and however irregularly it numbers annexes).

Its numbered entries ("10.8.40 SpecObject ..... 74", "Annex B: Interchange ..... 95") are parsed into
`TocEntry` values. An entry whose title wraps onto a second line (the number and the first part of
the title on one line, the rest and the leader on the next) is joined back together. Unnumbered
entries (a preface, a list of figures) are ignored. Clause detection uses the entries as the
authoritative list of clause numbers and titles.
"""

from __future__ import annotations

import re
from dataclasses import dataclass

from spec_extract.models import Line, ReconstructedPage

_TOC_TITLES = {"contents", "table of contents"}
_TOC_ENTRY = re.compile(r"\.{2,}\s*(?:\d+|[ivxlc]+)\s*$")
_TOC_ENTRY_DENSITY_THRESHOLD = 0.3
_ENTRY_START = re.compile(
    r"^(?:Annex\s+(?P<annex>[A-Z])\b[:.\-\u2013\u2014]?|(?P<number>\d+(?:\.\d+)*|[A-Z](?:\.\d+)+))\s+(?P<rest>\S.*)$"
)
_LEADER_AND_PAGE = re.compile(r"\s*\.{2,}\s*(?P<page>\d+|[ivxlc]+)\s*$")
_MAX_CONTINUATION_LINES = 2


@dataclass(frozen=True)
class TocEntry:
    """One numbered Table of Contents entry: the clause number, its full title and printed page."""

    number: str
    title: str
    printed_page: str


def is_toc_entry(line: Line) -> bool:
    """Whether `line` has a Table of Contents entry's structural shape: "... title ..... NN"."""
    return bool(_TOC_ENTRY.search(line.text.strip()))


def _toc_entry_density(page: ReconstructedPage) -> float:
    lines = [line for line in page.lines if line.text.strip()]
    if not lines:
        return 0.0
    return sum(1 for line in lines if is_toc_entry(line)) / len(lines)


def find_toc_page_span(pages: list[ReconstructedPage], hint: tuple[int, int] | None = None) -> tuple[int, int] | None:
    """The (first, last) indexes into `pages` of the Table of Contents, or `None` if there is none.

    `hint`, a profile's 1-based (first, last) PDF page numbers, is used as given when both pages exist."""
    if hint is not None:
        indexes = {page.number: index for index, page in enumerate(pages)}
        if hint[0] in indexes and hint[1] in indexes:
            return indexes[hint[0]], indexes[hint[1]]

    start = next(
        (
            index
            for index, page in enumerate(pages)
            if any(line.text.strip().lower() in _TOC_TITLES for line in page.lines)
        ),
        None,
    )
    if start is None:
        return None
    end = start
    while end + 1 < len(pages) and _toc_entry_density(pages[end + 1]) >= _TOC_ENTRY_DENSITY_THRESHOLD:
        end += 1
    return start, end


def _join_wrapped(parts: list[str]) -> str:
    """Join a wrapped title's parts, without a space after a line-ending hyphen ("Attribute-" "Value")."""
    title = ""
    for part in (part.strip() for part in parts):
        if not part:
            continue
        title = part if not title else title + part if title.endswith("-") else f"{title} {part}"
    return title


def parse_toc_entries(lines: list[Line]) -> list[TocEntry]:
    """Parse the numbered entries out of the Table of Contents' lines, joining wrapped titles."""
    entries: list[TocEntry] = []
    pending: tuple[str, list[str]] | None = None  # (number, title parts) of an entry still missing its leader
    continuation_lines = 0

    for line in lines:
        text = " ".join(line.text.split())
        start = _ENTRY_START.match(text)
        if start:
            number = start.group("annex") or start.group("number")
            pending, continuation_lines = (number, [start.group("rest")]), 0
        elif pending is not None and continuation_lines < _MAX_CONTINUATION_LINES:
            pending[1].append(text)
            continuation_lines += 1
        else:
            pending = None
            continue

        leader = _LEADER_AND_PAGE.search(pending[1][-1])
        if leader:
            number, parts = pending
            parts[-1] = parts[-1][: leader.start()]
            title = _join_wrapped(parts)
            entries.append(TocEntry(number=number, title=title, printed_page=leader.group("page")))
            pending = None

    return entries
