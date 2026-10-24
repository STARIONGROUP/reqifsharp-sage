# SPDX-FileCopyrightText: 2026 Starion Group S.A.
# SPDX-License-Identifier: Apache-2.0

"""Stage 3: skip the Table of Contents, then detect clause headings.

The Table of Contents (see `toc.py`) is both skipped and used. Its numbered entries are the
authoritative list of clauses: a body line is accepted as a clause heading only when (a) it starts
with a dotted number (e.g. "10.8.40 SpecObject") or "Annex X", (b) it is set in a visibly larger font
than the document's body text, (c) its number is a Table of Contents entry that comes after the
previously accepted heading's entry, and (d) its title matches that entry's title. The clause takes
the entry's title, which is complete even where the heading itself wraps onto a second line in the
body (that continuation line is consumed rather than kept as body text).

Without a parseable Table of Contents, (c) and (d) are replaced by a successor-numbering check: the
number must be a plausible successor of the previously accepted heading number in a depth-first
traversal of the section tree (a child, a sibling, or a sibling of some ancestor), and a heading's
wrapped continuation is recognised by being set in the same heading font.

The font gate matters either way: in a long technical document, numbered lists, table rows and
cross-references would otherwise produce a flood of false positives.

Annexes follow the last numbered clause and are numbered by letter: an "Annex B" heading (its title
either on the same line, "Annex B: Diagram Interchange", or on the next heading-sized line), then
lettered subclauses ("B.1 Overview", "B.5.2 Property Elements"). Each letter maps to a number past
every numbered clause (see `clause_number_key`), so the successor check also covers the step from the
last numbered clause to "Annex A". The "(normative)"/"(informative)" line under an annex title is
taken as that annex's designation, for the annex and all of its subclauses, rather than kept as body
text.
"""

from __future__ import annotations

import re
import statistics
from difflib import SequenceMatcher

from spec_extract.models import ANNEX_KEY_BASE, Clause, Line, ReconstructedPage, clause_number_key
from spec_extract.toc import TocEntry, find_toc_page_span, parse_toc_entries

_HEADING = re.compile(r"^(\d+(?:\.\d+)*|[A-Z](?:\.\d+)+)\s+(\S.*)$")
_ANNEX_HEADING = re.compile(r"^Annex\s+([A-Z])(?:\s*[:.\-\u2013\u2014]\s*|\s+|$)(.*)$")
_ANNEX_STATUS = re.compile(r"^\((normative|informative)\)$", re.IGNORECASE)
_HEADING_FONT_SIZE_MARGIN = 0.5
_CONTINUATION_FONT_SIZE_TOLERANCE = 0.25
_MIN_TOC_ENTRIES = 2
_TITLE_MATCH_RATIO = 0.85
_NON_ALPHANUMERIC = re.compile(r"[^0-9a-z]")


def _is_successor(previous: tuple[int, ...], candidate: tuple[int, ...]) -> bool:
    if candidate == previous + (1,):
        return True
    if candidate == (ANNEX_KEY_BASE,) and previous[0] < ANNEX_KEY_BASE:
        return True  # the last numbered clause is followed by "Annex A"
    for depth in range(1, len(previous) + 1):
        ancestor = previous[:depth]
        sibling = ancestor[:-1] + (ancestor[-1] + 1,)
        if candidate == sibling:
            return True
    return False


def _document_body_font_size(pages: list[ReconstructedPage]) -> float:
    """The most common word font size across the document - a proxy for "body text size"."""
    sizes = [word.size for page in pages for line in page.lines for word in line.words if word.size > 0]
    return statistics.mode(sizes) if sizes else 0.0


def _line_font_size(line: Line) -> float:
    sizes = [word.size for word in line.words if word.size > 0]
    return max(sizes) if sizes else 0.0


def _is_heading_sized(line: Line, body_font_size: float) -> bool:
    """Whether `line` is set in a visibly larger font than body text (always true without font data)."""
    if line.table is not None:
        return False
    if body_font_size <= 0:
        return True  # no font-size data available - regex only
    return _line_font_size(line) > body_font_size + _HEADING_FONT_SIZE_MARGIN


def _match_heading(line: Line, body_font_size: float) -> tuple[str, str] | None:
    """The (number, title) of a heading-shaped `line` - a clause number prefix or an "Annex X" heading,
    in a heading-sized font - or `None`. An annex heading whose title is on the next line has an
    empty title here."""
    if not _is_heading_sized(line, body_font_size):
        return None
    text = line.text.strip()
    annex = _ANNEX_HEADING.match(text)
    if annex:
        return annex.group(1), annex.group(2).strip()
    match = _HEADING.match(text)
    return (match.group(1), match.group(2).strip()) if match else None


def _squash(text: str) -> str:
    return _NON_ALPHANUMERIC.sub("", text.lower())


def _titles_match(body_title: str, toc_title: str) -> bool:
    """Whether a heading's (possibly wrapped, so truncated) title matches a Table of Contents title."""
    body, toc = _squash(body_title), _squash(toc_title)
    if not body:
        return True  # "Annex B" alone on its line: the title follows
    if toc.startswith(body) or body.startswith(toc):
        return True
    length = min(len(body), len(toc))
    return SequenceMatcher(None, body[:length], toc[:length]).ratio() >= _TITLE_MATCH_RATIO


class _TocGuide:
    """Accepts headings against the Table of Contents entries, in order."""

    def __init__(self, entries: list[TocEntry]) -> None:
        self.entries = entries
        self.max_depth = max(len(clause_number_key(entry.number)) for entry in entries)
        self._index_by_number: dict[str, int] = {}
        for index, entry in enumerate(entries):
            self._index_by_number.setdefault(entry.number, index)
        self.numbers = set(self._index_by_number)
        self._last_index = -1

    def accept(self, number: str, title: str) -> TocEntry | None:
        index = self._index_by_number.get(number)
        if index is None or index <= self._last_index or not _titles_match(title, self.entries[index].title):
            return None
        self._last_index = index
        return self.entries[index]


def _is_untabulated_subclause(guide: _TocGuide, last_number: tuple[int, ...] | None, number: str) -> bool:
    """Whether `number` is a subclause too deep for the Table of Contents to list (e.g. "3.2.3.1" in a
    contents list that stops at three levels) that plausibly follows the previously accepted heading."""
    candidate = clause_number_key(number)
    return (
        last_number is not None
        and len(candidate) > guide.max_depth
        and number not in guide.numbers
        and _is_successor(last_number, candidate)
    )


def _body_start_page_index(pages: list[ReconstructedPage], toc_span: tuple[int, int] | None) -> int:
    return 0 if toc_span is None else toc_span[1] + 1


def detect_clauses_and_toc(
    pages: list[ReconstructedPage], *, toc_pages: tuple[int, int] | None = None
) -> tuple[list[Clause], list[TocEntry]]:
    """Walk reading-ordered pages and split them into numbered clauses.

    Returns the clauses and the Table of Contents entries they were matched against (empty when the
    document has no parseable Table of Contents and the successor heuristic was used instead).
    `toc_pages` is a profile's 1-based (first, last) Table of Contents page range, if known."""
    body_font_size = _document_body_font_size(pages)
    toc_span = find_toc_page_span(pages, toc_pages)
    toc_entries: list[TocEntry] = []
    if toc_span is not None:
        toc_lines = [line for page in pages[toc_span[0] : toc_span[1] + 1] for line in page.lines]
        toc_entries = parse_toc_entries(toc_lines)
    guide = _TocGuide(toc_entries) if len(toc_entries) >= _MIN_TOC_ENTRIES else None

    clauses: list[Clause] = []
    current: Clause | None = None
    last_number: tuple[int, ...] | None = None
    annex_status: str | None = None
    heading_title = ""  # the current heading's title as printed in the body, for wrapped headings
    heading_size = 0.0
    title_from_toc = False

    for page in pages[_body_start_page_index(pages, toc_span) :]:
        page_number = page.number
        for line in page.lines:
            text = line.text.strip()
            heading = _match_heading(line, body_font_size)
            accepted: tuple[str, str] | None = None
            if heading is not None:
                number, title = heading
                if guide is not None:
                    entry = guide.accept(number, title)
                    if entry is not None:
                        accepted, title_from_toc = (number, entry.title or title), True
                    elif _is_untabulated_subclause(guide, last_number, number):
                        accepted, title_from_toc = heading, False
                elif last_number is None or _is_successor(last_number, clause_number_key(number)):
                    accepted, title_from_toc = heading, False

            if accepted is not None:
                if current is not None:
                    clauses.append(current)
                number, title = accepted
                if number.isalpha():
                    annex_status = None  # a new annex: its designation follows its title
                current = Clause(
                    number=number,
                    title=title,
                    page_start=page_number,
                    page_end=page_number,
                    annex_status=annex_status,
                )
                last_number = clause_number_key(number)
                heading_title = heading[1] if heading is not None else title
                heading_size = _line_font_size(line)
            elif current is None:
                continue  # front matter before the first clause
            elif current.is_annex and not current.raw_lines and (status := _ANNEX_STATUS.match(text)):
                annex_status = status.group(1).lower()
                current.annex_status = annex_status
                current.page_end = page_number
            elif current.is_annex and not current.raw_lines and not heading_title and text:
                # "Annex B" alone on its line: the title is the next line
                heading_title = text
                if not title_from_toc or not current.title:
                    current.title = text
                current.page_end = page_number
            elif not current.raw_lines and _continues_heading(
                line, text, current, heading_title, heading_size, title_from_toc, body_font_size
            ):
                heading_title = f"{heading_title} {text}"
                if not title_from_toc:
                    current.title = heading_title
                current.page_end = page_number
            else:
                current.raw_lines.append(line)
                current.page_end = page_number

    if current is not None:
        clauses.append(current)

    return clauses, toc_entries


def _continues_heading(
    line: Line,
    text: str,
    current: Clause,
    heading_title: str,
    heading_size: float,
    title_from_toc: bool,
    body_font_size: float,
) -> bool:
    """Whether `line`, directly under a heading, is that heading's wrapped title continuation."""
    if not heading_title or not text or not _is_heading_sized(line, body_font_size):
        return False
    if _match_heading(line, body_font_size) is not None:
        return False  # a (rejected) heading of its own, not a continuation
    if title_from_toc:
        so_far, full = _squash(heading_title), _squash(current.title)
        extended = _squash(f"{heading_title} {text}")
        return len(so_far) < len(full) and full.startswith(extended)
    return abs(_line_font_size(line) - heading_size) <= _CONTINUATION_FONT_SIZE_TOLERANCE


def detect_clauses(pages: list[ReconstructedPage], *, toc_pages: tuple[int, int] | None = None) -> list[Clause]:
    """Walk reading-ordered pages and split them into numbered clauses (see `detect_clauses_and_toc`)."""
    return detect_clauses_and_toc(pages, toc_pages=toc_pages)[0]
