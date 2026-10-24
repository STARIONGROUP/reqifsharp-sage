# SPDX-FileCopyrightText: 2026 Starion Group S.A.
# SPDX-License-Identifier: Apache-2.0

"""Stage 2: reconstruct reading order and strip running headers/footers.

A running header or footer is recognised two ways, both only inside a band at the top or bottom of
the page:

- automatically, as text that repeats on many pages once a leading or trailing page number (Arabic
  or lower-case Roman) is ignored - so "Requirements Interchange Format (ReqIF), v1.2 31" and
  "32 Requirements Interchange Format (ReqIF), v1.2" both count as the same footer - plus a bare page
  number that changes on every page;
- explicitly, by a document profile's `strip_patterns` (see `profiles.py`), for running text that
  does not repeat often enough to be detected, such as a short document's page footer.

Ruled tables found by stage 1 become one table "line" each, at the table's top.
"""

from __future__ import annotations

import re
from collections import Counter
from collections.abc import Iterable

from spec_extract.models import Line, PositionedPage, PositionedWord, ReconstructedPage

_LINE_TOLERANCE = 3.0
_CENTER_TOLERANCE_FRACTION = 0.45
_HEADER_BAND = 70.0
_FOOTER_BAND = 70.0
_MIN_REPEAT_COUNT = 3
_MIN_REPEAT_FRACTION = 0.3
_WHITESPACE = re.compile(r"\s+")
_PAGE_NUMBER = r"(?:\d+|[ivxlc]+)"
_LEADING_PAGE_NUMBER = re.compile(rf"^{_PAGE_NUMBER}\s+")
_TRAILING_PAGE_NUMBER = re.compile(rf"\s+{_PAGE_NUMBER}$")


def _normalize(text: str) -> str:
    return _WHITESPACE.sub(" ", text).strip().lower()


def _without_page_number(normalized: str) -> str:
    """`normalized` with a leading or trailing page-number token replaced by "#"."""
    stripped = _TRAILING_PAGE_NUMBER.sub(" #", normalized)
    return _LEADING_PAGE_NUMBER.sub("# ", stripped)


def _same_line(anchor: PositionedWord, word: PositionedWord, tolerance: float) -> bool:
    """Whether `word` sits on `anchor`'s line: tops within `tolerance`, or vertical centres within
    `_CENTER_TOLERANCE_FRACTION` of the smaller word height (a word in a different font, such as inline
    code, can sit a few points lower than its neighbours)."""
    if abs(anchor.top - word.top) <= tolerance:
        return True
    height = min(anchor.bottom - anchor.top, word.bottom - word.top)
    anchor_center = (anchor.top + anchor.bottom) / 2
    word_center = (word.top + word.bottom) / 2
    return height > 0 and abs(anchor_center - word_center) <= height * _CENTER_TOLERANCE_FRACTION


def _group_into_lines(words: tuple[PositionedWord, ...], tolerance: float) -> list[Line]:
    if not words:
        return []
    ordered = sorted(words, key=lambda word: word.top)
    clusters: list[list[PositionedWord]] = []
    for word in ordered:
        placed = False
        for cluster in clusters:
            if _same_line(cluster[0], word, tolerance):
                cluster.append(word)
                placed = True
                break
        if not placed:
            clusters.append([word])
    lines = [
        Line(top=min(w.top for w in cluster), words=tuple(sorted(cluster, key=lambda w: w.x0))) for cluster in clusters
    ]
    return sorted(lines, key=lambda line: line.top)


def _is_bare_number(text: str) -> bool:
    return _normalize(text).isdigit()


def _in_band(line: Line, page: PositionedPage, band: str) -> bool:
    return line.top <= _HEADER_BAND if band == "header" else line.top >= page.height - _FOOTER_BAND


def _find_repeated_band_text(
    pages: list[PositionedPage], lines_by_page: dict[int, list[Line]], *, band: str
) -> tuple[set[str], bool]:
    """Return (repeated page-number-normalized texts, numeric-footer-detected) for the header or footer band."""
    total_pages = len(pages)
    counter: Counter[str] = Counter()
    numeric_band_count = 0
    for page in pages:
        for line in lines_by_page[page.number]:
            normalized = _normalize(line.text)
            if not normalized or not _in_band(line, page, band):
                continue
            counter[_without_page_number(normalized)] += 1
            if band == "footer" and _is_bare_number(line.text):
                numeric_band_count += 1

    threshold = max(_MIN_REPEAT_COUNT, int(total_pages * _MIN_REPEAT_FRACTION))
    repeated = {text for text, count in counter.items() if count >= threshold and text != "#"}
    # A bare page number changes on every page, so it never repeats by exact text - detect it
    # by "most pages have *some* bare number in this band" instead.
    numeric_footer_detected = band == "footer" and numeric_band_count >= threshold
    return repeated, numeric_footer_detected


def _matches_any(text: str, patterns: Iterable[re.Pattern[str]]) -> bool:
    collapsed = _WHITESPACE.sub(" ", text).strip()
    return any(pattern.search(collapsed) for pattern in patterns)


def reconstruct_pages(
    pages: list[PositionedPage], *, strip_patterns: Iterable[re.Pattern[str]] = ()
) -> list[ReconstructedPage]:
    """Turn raw positioned words into reading-ordered lines, with running headers/footers stripped.

    `strip_patterns` are extra running header/footer patterns (see `DocumentProfile.strip_patterns`),
    applied only to lines inside the header and footer bands."""
    strip_patterns = tuple(strip_patterns)
    lines_by_page = {page.number: _group_into_lines(page.words, _LINE_TOLERANCE) for page in pages}
    repeated_header, _ = _find_repeated_band_text(pages, lines_by_page, band="header")
    repeated_footer, numeric_footer = _find_repeated_band_text(pages, lines_by_page, band="footer")

    reconstructed: list[ReconstructedPage] = []
    for page in pages:
        kept = []
        for line in lines_by_page[page.number]:
            in_header_band = _in_band(line, page, "header")
            in_footer_band = _in_band(line, page, "footer")
            key = _without_page_number(_normalize(line.text))
            if in_header_band and key in repeated_header:
                continue
            if in_footer_band and key in repeated_footer:
                continue
            if numeric_footer and in_footer_band and _is_bare_number(line.text):
                continue
            if (in_header_band or in_footer_band) and _matches_any(line.text, strip_patterns):
                continue
            kept.append(line)
        kept.extend(Line(top=table.top, words=(), table=table.rows) for table in page.tables)
        kept.sort(key=lambda line: line.top)
        reconstructed.append(ReconstructedPage(number=page.number, lines=tuple(kept)))
    return reconstructed
