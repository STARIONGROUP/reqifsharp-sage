# SPDX-FileCopyrightText: 2026 Starion Group S.A.
# SPDX-License-Identifier: Apache-2.0

from spec_extract.models import Line, PositionedWord, ReconstructedPage
from spec_extract.toc import find_toc_page_span, parse_toc_entries


def _line(text: str, top: float = 100) -> Line:
    word = PositionedWord(
        text=text, x0=72, x1=72 + len(text) * 6, top=top, bottom=top + 10, font_name="Helvetica", size=10.0
    )
    return Line(top=top, words=(word,))


def test_parse_toc_entries_reads_numbers_titles_and_printed_pages() -> None:
    entries = parse_toc_entries(
        [
            _line("Preface ........................ v"),
            _line("1 Scope ........................ 1"),
            _line("1.1 Audience ................... 1"),
            _line("10.8.40 Widget ................. 74"),
            _line("Formatting rules.......... 17"),
            _line("Annex A: Widgets ............... 95"),
            _line("A.1 Overview ................... 96"),
        ]
    )

    assert [(entry.number, entry.title, entry.printed_page) for entry in entries] == [
        ("1", "Scope", "1"),
        ("1.1", "Audience", "1"),
        ("10.8.40", "Widget", "74"),
        ("A", "Widgets", "95"),
        ("A.1", "Overview", "96"),
    ]


def test_parse_toc_entries_joins_a_title_wrapped_onto_the_next_line() -> None:
    entries = parse_toc_entries(
        [
            _line("7.2 How the format handles information from"),
            _line("the authoring tools ............ 11"),
            _line("7.3 How to resolve conflicting Gadget-"),
            _line("Definition values .............. 12"),
        ]
    )

    assert [(entry.number, entry.title) for entry in entries] == [
        ("7.2", "How the format handles information from the authoring tools"),
        ("7.3", "How to resolve conflicting Gadget-Definition values"),
    ]


def test_find_toc_page_span_follows_dense_entry_pages_after_the_contents_title() -> None:
    pages = [
        ReconstructedPage(number=1, lines=(_line("Front matter"),)),
        ReconstructedPage(number=2, lines=(_line("Contents"), _line("1 Scope ..... 1"), _line("2 Terms ..... 2"))),
        ReconstructedPage(number=3, lines=(_line("3 Rules ..... 3"), _line("4 More ..... 4"))),
        ReconstructedPage(number=4, lines=(_line("Body text."), _line("More body text."))),
    ]

    assert find_toc_page_span(pages) == (1, 2)


def test_find_toc_page_span_uses_a_profile_hint_when_its_pages_exist() -> None:
    pages = [ReconstructedPage(number=number, lines=(_line("Body text."),)) for number in range(1, 6)]

    assert find_toc_page_span(pages, hint=(2, 3)) == (1, 2)
    assert find_toc_page_span(pages, hint=(9, 10)) is None  # no such pages, and no "Contents" page either
