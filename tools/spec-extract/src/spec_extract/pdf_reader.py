"""Stage 1: raw positioned words and ruled tables per page. No layout logic lives here."""

from __future__ import annotations

import re
from pathlib import Path

import pdfplumber

from spec_extract.models import PositionedPage, PositionedTable, PositionedWord

_MIN_TABLE_ROWS = 2
_MIN_TABLE_COLUMNS = 2
_MAX_FRAGMENT_CELL_FRACTION = 0.6
_FRAGMENT_TOKEN_LENGTH = 2.5
_HYPHEN_LINE_BREAK = re.compile(r"(?<=\w)-\n(?=\w)")


# Symbol/Wingdings glyphs that decode into the Unicode Private Use Area, mapped to what they draw.
_PRIVATE_USE_GLYPHS = str.maketrans({"\uf0b7": "\u2022", "\uf0a7": "\u25aa", "\uf0e0": "\u2192", "\uf0e8": "\u2192"})


def _clean_text(text: str) -> str:
    return text.translate(_PRIVATE_USE_GLYPHS)


def _clean_cell(cell: str | None) -> str:
    """A cell's text on one line; a line break after a hyphen ("ReqIF-" "Chng-1") is joined without a space."""
    return " ".join(_HYPHEN_LINE_BREAK.sub("-", _clean_text(cell or "")).split())


def _merge_spanned_columns(raw_rows: list[list[str | None]]) -> list[list[str]]:
    """Fold the grid columns that a merged cell spans back into one logical column.

    pdfplumber reports the part of a row covered by a merged cell as `None`. A grid column that is
    `None` in most rows is a continuation of the logical column to its left; text in it (typically a
    header cell centred over the span) is appended to that logical column's cell."""
    width = max((len(row) for row in raw_rows), default=0)
    rows = [row + [None] * (width - len(row)) for row in raw_rows]
    starts = [
        index for index in range(width) if index == 0 or sum(1 for row in rows if row[index] is None) * 2 <= len(rows)
    ]
    spans = list(zip(starts, [*starts[1:], width]))
    return [
        [" ".join(text for text in (_clean_cell(cell) for cell in row[start:end]) if text) for start, end in spans]
        for row in rows
    ]


def _clean_rows(raw_rows: list[list[str | None]]) -> tuple[tuple[str, ...], ...]:
    """Whitespace-normalize every cell, fold merged-cell spans, then drop rows and columns that are
    empty throughout."""
    rows = _merge_spanned_columns(raw_rows)
    rows = [row for row in rows if any(row)]
    if not rows:
        return ()
    width = max(len(row) for row in rows)
    rows = [row + [""] * (width - len(row)) for row in rows]
    kept_columns = [index for index in range(width) if any(row[index] for row in rows)]
    return tuple(tuple(row[index] for index in kept_columns) for row in rows)


def _is_plausible_table(rows: tuple[tuple[str, ...], ...]) -> bool:
    """Whether `rows` look like a real table rather than a diagram's boxes that happen to be ruled
    (those split labels into fragments of a letter or two: "d", "e s", "tri")."""
    if len(rows) < _MIN_TABLE_ROWS or len(rows[0]) < _MIN_TABLE_COLUMNS:
        return False
    cells = [cell for row in rows for cell in row if cell]
    fragments = sum(1 for cell in cells if _is_fragment(cell))
    return bool(cells) and fragments / len(cells) <= _MAX_FRAGMENT_CELL_FRACTION


def _is_fragment(cell: str) -> bool:
    tokens = cell.split()
    return sum(len(token) for token in tokens) / len(tokens) <= _FRAGMENT_TOKEN_LENGTH


def _inside(word: dict, bbox: tuple[float, float, float, float]) -> bool:
    x0, top, x1, bottom = bbox
    center_x = (word["x0"] + word["x1"]) / 2
    center_y = (word["top"] + word["bottom"]) / 2
    return x0 <= center_x <= x1 and top <= center_y <= bottom


def read_positioned_pages(pdf_path: str | Path, *, detect_tables: bool = True) -> list[PositionedPage]:
    """Open a PDF and extract every word's text and bounding box, page by page.

    With `detect_tables`, ruled tables are extracted as `PositionedTable` blocks and their words are
    left out of the page's loose words."""
    pages: list[PositionedPage] = []
    with pdfplumber.open(pdf_path) as pdf:
        for index, page in enumerate(pdf.pages, start=1):
            raw_words = page.extract_words(extra_attrs=["fontname", "size"], use_text_flow=False)

            tables: list[PositionedTable] = []
            table_boxes: list[tuple[float, float, float, float]] = []
            if detect_tables:
                for found in page.find_tables():
                    rows = _clean_rows(found.extract())
                    if _is_plausible_table(rows):
                        tables.append(PositionedTable(top=found.bbox[1], bottom=found.bbox[3], rows=rows))
                        table_boxes.append(found.bbox)

            words = tuple(
                PositionedWord(
                    text=_clean_text(raw["text"]),
                    x0=raw["x0"],
                    x1=raw["x1"],
                    top=raw["top"],
                    bottom=raw["bottom"],
                    font_name=raw.get("fontname", ""),
                    size=raw.get("size", 0.0),
                )
                for raw in raw_words
                if not any(_inside(raw, box) for box in table_boxes)
            )
            pages.append(PositionedPage(number=index, height=float(page.height), words=words, tables=tuple(tables)))
    return pages
