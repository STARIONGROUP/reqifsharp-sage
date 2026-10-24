# SPDX-FileCopyrightText: 2026 Starion Group S.A.
# SPDX-License-Identifier: Apache-2.0

import re

from spec_extract.models import Clause, Line, PositionedWord
from spec_extract.normative import document_line_spacing, split_normative


def _line(text: str, top: float) -> Line:
    word = PositionedWord(
        text=text, x0=72, x1=72 + len(text) * 6, top=top, bottom=top + 10, font_name="Helvetica", size=10.0
    )
    return Line(top=top, words=(word,))


def _clause(lines: list[Line]) -> Clause:
    clause = Clause(number="1", title="Test", page_start=1, page_end=1)
    clause.raw_lines = lines
    return clause


def test_split_normative_groups_close_lines_into_one_paragraph_and_splits_on_a_big_gap() -> None:
    clause = _clause(
        [
            _line("A classifier is a classification", 100),
            _line("of instances.", 112),
            _line("It describes structural features.", 160),  # big gap before this line
        ]
    )

    split_normative(clause)

    assert len(clause.paragraphs) == 2
    assert clause.paragraphs[0].text == "A classifier is a classification of instances."
    assert clause.paragraphs[1].text == "It describes structural features."


def test_split_normative_flags_shall_and_must_as_normative() -> None:
    clause = _clause(
        [
            _line("A Class shall have a", 100),
            _line("name.", 112),  # small gap: same paragraph as the line above
            _line("A description may be provided.", 160),  # big gap: a new, non-normative paragraph
        ]
    )

    split_normative(clause)

    assert len(clause.paragraphs) == 2
    assert clause.paragraphs[0].text == "A Class shall have a name."
    assert clause.paragraphs[0].is_normative is True
    assert clause.paragraphs[1].is_normative is False
    assert clause.is_normative is True


def test_split_normative_tags_note_and_example_as_informative_and_not_normative() -> None:
    clause = _clause(
        [
            _line("A Class must have a name.", 100),
            _line("NOTE This is an informative note that also says shall.", 112),
            _line("EXAMPLE Consider a Class named Car.", 124),
        ]
    )

    split_normative(clause)

    assert len(clause.paragraphs) == 3
    assert clause.paragraphs[0].is_normative is True
    assert clause.paragraphs[1].informative_kind == "note"
    assert clause.paragraphs[1].is_normative is False
    assert clause.paragraphs[2].informative_kind == "example"


def _styled(text: str, top: float, font: str, x0: float = 72) -> Line:
    words = []
    for token in text.split():
        words.append(
            PositionedWord(text=token, x0=x0, x1=x0 + len(token) * 6, top=top, bottom=top + 10, font_name=font, size=10)
        )
        x0 += len(token) * 6 + 4
    return Line(top=top, words=tuple(words))


def _labelled(label: str, value: str, top: float) -> Line:
    """A line whose first word is a bold run-in label ("Kind:") and the rest regular text."""
    bold = _styled(label, top, "Helvetica-Bold")
    rest = _styled(value, top, "Helvetica", x0=72 + len(label) * 6 + 4)
    return Line(top=top, words=bold.words + rest.words)


def test_split_normative_uses_the_document_line_spacing_when_given() -> None:
    # every gap here is 20pt: on its own the clause looks like one paragraph, but at a 12pt document
    # line spacing each line is a paragraph of its own
    clause = _clause([_line("First paragraph.", 100), _line("Second paragraph.", 120), _line("Third.", 140)])

    split_normative(clause, line_spacing=12)

    assert [paragraph.text for paragraph in clause.paragraphs] == ["First paragraph.", "Second paragraph.", "Third."]


def test_document_line_spacing_is_the_smallest_common_body_gap_not_the_most_common() -> None:
    page = [_line(f"Line {index}", top) for index, top in enumerate([100, 112, 124, 144, 164, 184, 204, 224, 244])]

    assert document_line_spacing([page], body_font_size=10.0) == 12


def test_split_normative_renders_bold_labels_and_run_in_labels_as_their_own_paragraphs() -> None:
    clause = _clause(
        [
            _labelled("Kind:", "Widget", 100),
            _labelled("isAbstract:", "No", 113),
            _styled("Description", 135, "Helvetica-Bold"),
            _line("A widget is a thing.", 148),
        ]
    )

    split_normative(clause, line_spacing=12)

    assert [paragraph.text for paragraph in clause.paragraphs] == [
        "**Kind:** Widget",
        "**isAbstract:** No",
        "**Description**",
        "A widget is a thing.",
    ]


def test_split_normative_renders_bullets_as_markdown_list_items_and_joins_hyphenated_line_breaks() -> None:
    clause = _clause(
        [
            _line("• A non-", 100),
            _line("trivial item.", 112),
            _line("• Another item.", 124),
        ]
    )

    split_normative(clause, line_spacing=12)

    assert [paragraph.text for paragraph in clause.paragraphs] == ["- A non-trivial item.", "- Another item."]


def test_split_normative_renders_monospaced_lines_as_one_indented_code_block() -> None:
    clause = _clause(
        [
            _line("The example:", 100),
            _styled("<root>", 120, "Courier"),
            _styled("<child/>", 140, "Courier", x0=72 + 2 * 6),  # wider gaps than prose, still one block
            _styled("</root>", 160, "Courier"),
            _line("That was the example.", 180),
        ]
    )

    split_normative(clause, line_spacing=12)

    assert [paragraph.text for paragraph in clause.paragraphs] == [
        "The example:",
        "```\n<root>\n  <child/>\n</root>\n```",
        "That was the example.",
    ]


def test_split_normative_renders_a_table_and_joins_its_continuation_on_the_next_page() -> None:
    header = ("Name", "Value")
    clause = _clause(
        [
            _line("The table:", 100),
            Line(top=700, words=(), table=(header, ("a", "1"))),
            Line(top=80, words=(), table=(header, ("b", "x|y"))),  # next page, header repeated
            _line("After the table.", 200),
        ]
    )

    split_normative(clause, line_spacing=12)

    assert [paragraph.text for paragraph in clause.paragraphs] == [
        "The table:",
        "| Name | Value |\n|---|---|\n| a | 1 |\n| b | x\\|y |",
        "After the table.",
    ]


def test_split_normative_with_no_normative_pattern_marks_recommendations_but_nothing_normative() -> None:
    recommendation = re.compile(r"\bshould\b")
    clause = _clause(
        [
            _line("Tools must and should do this.", 100),
            _line("Background information.", 140),
            Line(top=180, words=(), table=(("Kind", "Recommended"), ("a", "should"))),
        ]
    )

    split_normative(clause, line_spacing=12, normative_pattern=None, recommendation_pattern=recommendation)

    assert clause.is_normative is False
    assert [paragraph.is_recommendation for paragraph in clause.paragraphs] == [True, False, False]
