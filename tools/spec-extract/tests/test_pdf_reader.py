# SPDX-FileCopyrightText: 2026 Starion Group S.A.
# SPDX-License-Identifier: Apache-2.0

from pathlib import Path

from spec_extract.pdf_reader import _clean_rows, _is_plausible_table, read_positioned_pages
from tests.pdf_fixtures import Page, Rule, Text, build_pdf, build_simple_pdf


def test_read_positioned_pages_extracts_text_and_positions(tmp_path: Path) -> None:
    pdf_path = tmp_path / "sample.pdf"
    pdf_path.write_bytes(build_simple_pdf(["Hello World", "Second Line"]))

    pages = read_positioned_pages(pdf_path)

    assert len(pages) == 1
    page = pages[0]
    assert page.number == 1
    assert page.height == 792.0

    texts = [word.text for word in page.words]
    assert "Hello" in texts
    assert "World" in texts
    assert "Second" in texts
    assert "Line" in texts


def test_read_positioned_pages_words_have_increasing_top_per_line(tmp_path: Path) -> None:
    pdf_path = tmp_path / "sample.pdf"
    pdf_path.write_bytes(build_simple_pdf(["First", "Second"], start_y=700, line_height=50))

    pages = read_positioned_pages(pdf_path)
    words = sorted(pages[0].words, key=lambda word: word.top)

    assert words[0].text == "First"
    assert words[1].text == "Second"
    assert words[0].top < words[1].top


def test_read_positioned_pages_reports_each_page_with_its_own_height_and_fonts(tmp_path: Path) -> None:
    pdf_path = tmp_path / "pages.pdf"
    pdf_path.write_bytes(
        build_pdf(
            [
                Page(items=[Text("Portrait", 700, font="bold")]),
                Page(items=[Text("Landscape", 500, font="mono")], width=842, height=595),
            ]
        )
    )

    pages = read_positioned_pages(pdf_path)

    assert [(page.number, page.height) for page in pages] == [(1, 792.0), (2, 595.0)]
    assert "Bold" in pages[0].words[0].font_name
    assert "Courier" in pages[1].words[0].font_name


def _grid(left: float, top: float, column_width: float, row_height: float, columns: int, rows: int) -> list[Rule]:
    right, bottom = left + columns * column_width, top - rows * row_height
    rules = [Rule(left, top - index * row_height, right, top - index * row_height) for index in range(rows + 1)]
    rules += [
        Rule(left + index * column_width, top, left + index * column_width, bottom) for index in range(columns + 1)
    ]
    return rules


def test_read_positioned_pages_extracts_a_ruled_table_and_drops_its_words_from_the_prose(tmp_path: Path) -> None:
    cells = [("Name", "Meaning"), ("alpha", "first letter"), ("beta", "second letter")]
    items: list[Text | Rule] = [Text("Text before the table.", 720)]
    items += _grid(72, 700, 200, 20, columns=2, rows=3)
    for row_index, row in enumerate(cells):
        for column_index, cell in enumerate(row):
            items.append(Text(cell, 700 - row_index * 20 - 14, x=76 + column_index * 200, size=10))
    items.append(Text("Text after the table.", 600))
    pdf_path = tmp_path / "table.pdf"
    pdf_path.write_bytes(build_pdf([Page(items=items)]))

    page = read_positioned_pages(pdf_path)[0]

    assert len(page.tables) == 1
    assert page.tables[0].rows == tuple(cells)
    assert {word.text for word in page.words} == {"Text", "before", "the", "table.", "after"}


def test_clean_rows_folds_merged_cell_spans_and_drops_empty_columns() -> None:
    raw = [
        ["", "Kind", "", "", "Text", ""],
        ["Bold", None, None, "b", None, None],
        ["Italic", None, None, "i", None, None],
    ]

    assert _clean_rows(raw) == (("Kind", "Text"), ("Bold", "b"), ("Italic", "i"))


def test_clean_rows_joins_a_line_break_after_a_hyphen_without_a_space() -> None:
    assert _clean_rows([["Chng-\n1", "two\nlines"], ["a", "b"]]) == (("Chng-1", "two lines"), ("a", "b"))


def test_is_plausible_table_rejects_a_diagram_split_into_letter_fragments() -> None:
    diagram = (("+", "d", "e s", "c:", "tri", "n g"), ("i l l", "d a o", "e st n", "fi a a", "er n m", ": g"))
    real = (("Step ID", "Step description"), ("1.0", "Export the file"), ("2.0", "Transmit the file"))

    assert not _is_plausible_table(diagram)
    assert _is_plausible_table(real)
    assert not _is_plausible_table((("only", "one row"),))
