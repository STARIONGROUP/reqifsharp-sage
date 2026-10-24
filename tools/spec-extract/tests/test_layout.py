# SPDX-FileCopyrightText: 2026 Starion Group S.A.
# SPDX-License-Identifier: Apache-2.0

import re

from spec_extract.layout import reconstruct_pages
from spec_extract.models import PositionedPage, PositionedTable, PositionedWord


def _word(text: str, x0: float, top: float) -> PositionedWord:
    return PositionedWord(
        text=text, x0=x0, x1=x0 + len(text) * 6, top=top, bottom=top + 10, font_name="Helvetica", size=10.0
    )


def test_reconstruct_pages_orders_words_into_lines_top_to_bottom_left_to_right() -> None:
    page = PositionedPage(
        number=1,
        height=792.0,
        words=(
            _word("World", 130, 200),
            _word("Hello", 72, 200),
            _word("Second", 72, 220),
        ),
    )

    reconstructed = reconstruct_pages([page])

    assert len(reconstructed) == 1
    lines = reconstructed[0].lines
    assert [line.text for line in lines] == ["Hello World", "Second"]


def test_reconstruct_pages_strips_repeated_header_and_changing_page_number_footer() -> None:
    pages = []
    for number in range(1, 5):
        words = (
            _word("Header", 72, 20),
            _word("Body", 72, 400),
            _word(str(41 + number), 300, 780),  # page number changes every page, unlike the header
        )
        pages.append(PositionedPage(number=number, height=792.0, words=words))

    reconstructed = reconstruct_pages(pages)

    for page in reconstructed:
        texts = [line.text for line in page.lines]
        assert texts == ["Body"]


def _line_words(text: str, top: float, x0: float = 72) -> tuple[PositionedWord, ...]:
    words = []
    for token in text.split():
        words.append(_word(token, x0, top))
        x0 += len(token) * 6 + 4
    return tuple(words)


def _guide_like_page(number: int, height: float) -> PositionedPage:
    """A synthetic page in the layout of an implementation guide: a two-line running header (a title
    and a release line) and a two-line footer whose first line carries the page number."""
    words = (
        *_line_words("Example Guide", 38),
        *_line_words("Release 9.9, 1 January 2030", 61),
        *_line_words(f"Body text on page {number}.", 300),
        *_line_words(f"Relating to Thing 4.2 {number}", height - 56),
        *_line_words("Example Association All rights kept", height - 44),
    )
    return PositionedPage(number=number, height=height, words=words)


def test_reconstruct_pages_strips_a_guide_like_two_line_header_and_numbered_footer_on_mixed_page_sizes() -> None:
    # portrait and landscape pages: the footer band is measured from each page's own bottom
    pages = [_guide_like_page(number, 595.0 if number == 3 else 842.0) for number in range(1, 7)]

    reconstructed = reconstruct_pages(pages)

    for page in reconstructed:
        assert [line.text for line in page.lines] == [f"Body text on page {page.number}."]


def test_reconstruct_pages_strips_a_footer_whose_page_number_alternates_sides() -> None:
    pages = []
    for number in range(1, 7):
        footer = f"{number} Example Format, v9" if number % 2 == 0 else f"Example Format, v9 {number}"
        words = (*_line_words("Body", 300), *_line_words(footer, 733))
        pages.append(PositionedPage(number=number, height=792.0, words=words))

    reconstructed = reconstruct_pages(pages)

    assert all([line.text for line in page.lines] == ["Body"] for page in reconstructed)


def test_reconstruct_pages_keeps_repeated_text_outside_the_header_and_footer_bands() -> None:
    pages = [
        PositionedPage(
            number=number,
            height=792.0,
            words=(*_line_words("No additional information", 300), *_line_words("No additional information", 30)),
        )
        for number in range(1, 6)
    ]

    reconstructed = reconstruct_pages(pages)

    assert all([line.text for line in page.lines] == ["No additional information"] for page in reconstructed)


def test_reconstruct_pages_applies_profile_strip_patterns_only_inside_the_bands() -> None:
    pattern = re.compile(r"^Release \d+(?:\.\d+)*, \d{1,2} \w+ \d{4}$")
    pages = [
        PositionedPage(
            number=1,
            height=792.0,
            words=(*_line_words("Release 2.0, 1 January 2030", 40), *_line_words("Release 2.0, 1 January 2030", 300)),
        )
    ]

    reconstructed = reconstruct_pages(pages, strip_patterns=[pattern])

    assert [line.top for line in reconstructed[0].lines] == [300]


def test_reconstruct_pages_inserts_a_table_as_one_line_in_reading_order() -> None:
    page = PositionedPage(
        number=1,
        height=792.0,
        words=(*_line_words("Before", 100), *_line_words("After", 400)),
        tables=(PositionedTable(top=200, bottom=300, rows=(("Name", "Value"), ("a", "1"))),),
    )

    lines = reconstruct_pages([page])[0].lines

    assert [line.text for line in lines] == ["Before", "Name Value a 1", "After"]
    assert lines[1].table == (("Name", "Value"), ("a", "1"))


def test_reconstruct_pages_keeps_inline_text_set_a_few_points_lower_on_its_line() -> None:
    words = (
        _word("The", 72, 85),
        _word("value", 100, 85),
        PositionedWord(text="_id42", x0=140, x1=170, top=89, bottom=99, font_name="Courier", size=10.0),
        _word("is", 175, 85),
        _word("Next", 72, 98),
    )

    lines = reconstruct_pages([PositionedPage(number=1, height=792.0, words=words)])[0].lines

    assert [line.text for line in lines] == ["The value _id42 is", "Next"]
