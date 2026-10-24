# SPDX-FileCopyrightText: 2026 Starion Group S.A.
# SPDX-License-Identifier: Apache-2.0

"""Minimal, hand-written (not derived from any real specification) PDF builders for the tests.

Avoids pulling in a PDF-generation dependency just to test PDF *reading*: this writes the smallest
valid PDF structure by hand - pages of positioned text lines in the standard Helvetica,
Helvetica-Bold and Courier fonts, plus optional ruled lines (so pdfplumber finds tables).
"""

from __future__ import annotations

from dataclasses import dataclass, field

FONTS = {"regular": "F1", "bold": "F2", "mono": "F3"}
_BASE_FONTS = {"F1": "Helvetica", "F2": "Helvetica-Bold", "F3": "Courier"}


@dataclass(frozen=True)
class Text:
    """One line of text, its baseline at (`x`, `y`) in PDF points from the bottom-left corner."""

    text: str
    y: float
    x: float = 72
    size: float = 12.0
    font: str = "regular"  # "regular" | "bold" | "mono"


@dataclass(frozen=True)
class Rule:
    """A straight ruled line from (`x0`, `y0`) to (`x1`, `y1`), e.g. a table border."""

    x0: float
    y0: float
    x1: float
    y1: float


@dataclass
class Page:
    items: list[Text | Rule] = field(default_factory=list)
    width: float = 612
    height: float = 792


def _escape(text: str) -> str:
    return text.replace("\\", r"\\").replace("(", r"\(").replace(")", r"\)")


def _content_stream(page: Page) -> bytes:
    commands = []
    for item in page.items:
        if isinstance(item, Text):
            commands.append(
                f"BT /{FONTS[item.font]} {item.size:.1f} Tf {item.x:.1f} {item.y:.1f} Td ({_escape(item.text)}) Tj ET"
            )
        else:
            commands.append(f"0.5 w {item.x0:.1f} {item.y0:.1f} m {item.x1:.1f} {item.y1:.1f} l S")
    return "\n".join(commands).encode("latin-1")


def build_pdf(pages: list[Page]) -> bytes:
    """A multi-page PDF of the given pages."""
    font_ids = {name: 3 + index for index, name in enumerate(_BASE_FONTS)}
    first_page_id = 3 + len(_BASE_FONTS)
    page_ids = [first_page_id + 2 * index for index in range(len(pages))]

    objects: dict[int, bytes] = {
        1: b"<< /Type /Catalog /Pages 2 0 R >>",
        2: f"<< /Type /Pages /Kids [{' '.join(f'{pid} 0 R' for pid in page_ids)}] /Count {len(pages)} >>".encode(
            "latin-1"
        ),
    }
    for name, object_id in font_ids.items():
        objects[object_id] = f"<< /Type /Font /Subtype /Type1 /BaseFont /{_BASE_FONTS[name]} >>".encode("latin-1")
    fonts = " ".join(f"/{name} {object_id} 0 R" for name, object_id in font_ids.items())
    for page, page_id in zip(pages, page_ids):
        stream = _content_stream(page)
        objects[page_id] = (
            f"<< /Type /Page /Parent 2 0 R /Resources << /Font << {fonts} >> >> "
            f"/MediaBox [0 0 {page.width:.0f} {page.height:.0f}] /Contents {page_id + 1} 0 R >>"
        ).encode("latin-1")
        objects[page_id + 1] = b"<< /Length %d >>\nstream\n" % len(stream) + stream + b"\nendstream"

    buffer = bytearray(b"%PDF-1.4\n")
    offsets = {}
    for object_id in sorted(objects):
        offsets[object_id] = len(buffer)
        buffer += f"{object_id} 0 obj\n".encode("latin-1") + objects[object_id] + b"\nendobj\n"

    count = max(objects) + 1
    xref_offset = len(buffer)
    buffer += f"xref\n0 {count}\n".encode("latin-1")
    buffer += b"0000000000 65535 f \n"
    for object_id in range(1, count):
        buffer += f"{offsets[object_id]:010d} 00000 n \n".encode("latin-1")
    buffer += f"trailer\n<< /Size {count} /Root 1 0 R >>\nstartxref\n{xref_offset}\n%%EOF".encode("latin-1")
    return bytes(buffer)


def build_simple_pdf(
    lines: list[str],
    *,
    start_y: float = 700,
    line_height: float = 20,
    x: float = 72,
    page_width: float = 612,
    page_height: float = 792,
    sizes: list[float] | None = None,
) -> bytes:
    """A single page of left-aligned Helvetica lines. `sizes`, when given, is a per-line font size
    (defaulting to 12pt for any line it omits) - used to give heading lines a distinctly larger size
    than body text, matching real documents."""
    items: list[Text | Rule] = [
        Text(text=line, y=start_y - index * line_height, x=x, size=sizes[index] if sizes and index < len(sizes) else 12)
        for index, line in enumerate(lines)
    ]
    return build_pdf([Page(items=items, width=page_width, height=page_height)])
