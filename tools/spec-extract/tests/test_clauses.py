# SPDX-FileCopyrightText: 2026 Starion Group S.A.
# SPDX-License-Identifier: Apache-2.0

from spec_extract.clauses import detect_clauses, detect_clauses_and_toc
from spec_extract.models import Line, PositionedWord, ReconstructedPage, clause_number_key
from spec_extract.normative import split_normative

_BODY_SIZE = 10.0
_HEADING_SIZE = 14.0


def _line(text: str, top: float, size: float = _BODY_SIZE) -> Line:
    word = PositionedWord(
        text=text, x0=72, x1=72 + len(text) * 6, top=top, bottom=top + 10, font_name="Helvetica", size=size
    )
    return Line(top=top, words=(word,))


def _heading(text: str, top: float) -> Line:
    return _line(text, top, size=_HEADING_SIZE)


def test_detect_clauses_accepts_a_plausible_successor_sequence() -> None:
    pages = [
        ReconstructedPage(
            number=1,
            lines=(
                _heading("1 Scope", 100),
                _line("This clause defines scope.", 120),
                _line("It has a second sentence too.", 140),
                _heading("1.1 Overview", 160),
                _line("This is the overview body.", 180),
                _line("It also has a second sentence.", 200),
                _heading("2 Conformance", 220),
                _line("This is the conformance body.", 240),
                _line("It also has a second sentence.", 260),
            ),
        )
    ]

    clauses = detect_clauses(pages)

    assert [clause.number for clause in clauses] == ["1", "1.1", "2"]
    assert clauses[0].title == "Scope"
    assert [line.text for line in clauses[0].raw_lines] == [
        "This clause defines scope.",
        "It has a second sentence too.",
    ]
    assert clauses[2].title == "Conformance"


def test_detect_clauses_rejects_a_non_successor_number_as_body_text() -> None:
    pages = [
        ReconstructedPage(
            number=1,
            lines=(
                _heading("9.3.2 Classifier", 100),
                _line("3 apples were purchased that day.", 120),
                _line("A classifier is a classification of instances.", 140),
            ),
        )
    ]

    clauses = detect_clauses(pages)

    assert len(clauses) == 1
    assert clauses[0].number == "9.3.2"
    assert [line.text for line in clauses[0].raw_lines] == [
        "3 apples were purchased that day.",
        "A classifier is a classification of instances.",
    ]


def test_detect_clauses_rejects_a_same_font_stray_number_as_body_text() -> None:
    """Even a same-font, heading-shaped line is rejected when it isn't a plausible successor -
    the font gate alone isn't the only defense against false positives."""
    pages = [
        ReconstructedPage(
            number=1,
            lines=(
                _heading("9.3.2 Classifier", 100),
                _line("A classifier is a classification of instances.", 120),
                _line("It describes structural and behavioral features.", 140),
                _line("Classifiers may be generalized.", 160),
                _heading("3 See section 3 for the full grammar.", 180),
            ),
        )
    ]

    clauses = detect_clauses(pages)

    assert len(clauses) == 1
    assert [line.text for line in clauses[0].raw_lines] == [
        "A classifier is a classification of instances.",
        "It describes structural and behavioral features.",
        "Classifiers may be generalized.",
        "3 See section 3 for the full grammar.",
    ]


def test_detect_clauses_ignores_a_body_sized_line_that_matches_the_heading_pattern() -> None:
    """A body-sized line that happens to start with a dotted number (e.g. a numbered list item in
    prose) is never treated as a heading - only a visibly larger font qualifies."""
    pages = [
        ReconstructedPage(
            number=1,
            lines=(
                _heading("1 Scope", 100),
                _line("2 See the numbered list below for details.", 120),
                _line("More scope body text.", 140),
            ),
        )
    ]

    clauses = detect_clauses(pages)

    assert len(clauses) == 1
    assert [line.text for line in clauses[0].raw_lines] == [
        "2 See the numbered list below for details.",
        "More scope body text.",
    ]


def test_detect_clauses_skips_a_table_of_contents_spanning_multiple_pages_with_irregular_annex_numbering() -> None:
    pages = [
        ReconstructedPage(
            number=1,
            lines=(
                _line("Contents", 50),
                _line("1 Scope ..... 3", 80),
                _line("1.1 Overview ..... 4", 100),
                _line("2 Conformance ..... 10", 120),
            ),
        ),
        ReconstructedPage(
            number=2,
            lines=(
                _line("Annex A: Diagrams ..... 300", 80),
                _line("Annex B: Diagram Interchange ..... 320", 100),
                _line("B.1 Summary ..... 321", 120),
                _line("B.7.3 AssociationShape [Class] ..... 355", 140),
            ),
        ),
        ReconstructedPage(
            number=3,
            lines=(
                _heading("1 Scope", 100),
                _line("Real scope body line one.", 120),
                _line("Real scope body line two.", 140),
                _line("Real scope body line three.", 160),
                _line("Real scope body line four.", 180),
                _heading("1.1 Overview", 200),
                _line("Real overview body line one.", 220),
                _line("Real overview body line two.", 240),
                _line("Real overview body line three.", 260),
                _line("Real overview body line four.", 280),
            ),
        ),
    ]

    clauses = detect_clauses(pages)

    assert [clause.number for clause in clauses] == ["1", "1.1"]
    assert clauses[0].page_start == 3
    assert [line.text for line in clauses[0].raw_lines] == [
        "Real scope body line one.",
        "Real scope body line two.",
        "Real scope body line three.",
        "Real scope body line four.",
    ]


def test_detect_clauses_tracks_page_span_across_page_boundary() -> None:
    pages = [
        ReconstructedPage(number=1, lines=(_heading("1 Scope", 100), _line("Body on page one.", 120))),
        ReconstructedPage(number=2, lines=(_line("More body on page two.", 100),)),
    ]

    clauses = detect_clauses(pages)

    assert len(clauses) == 1
    assert clauses[0].page_start == 1
    assert clauses[0].page_end == 2


def test_detect_clauses_continues_from_the_last_numbered_clause_into_lettered_annexes() -> None:
    pages = [
        ReconstructedPage(
            number=1,
            lines=(
                _heading("10.3 Mapping", 100),
                _line("Mapping body line one.", 120),
                _line("Mapping body line two.", 140),
                _line("Mapping body line three.", 160),
                _line("Mapping body line four.", 180),
                _line("Mapping body line five.", 200),
                _line("Mapping body line six.", 220),
                _line("Mapping body line seven.", 240),
                _heading("10.4 Example", 260),
                _line("Example body.", 280),
            ),
        ),
        ReconstructedPage(
            number=2,
            lines=(
                _heading("Annex A", 100),
                _heading("Bibliography", 120),
                _line("(informative)", 140),
                _line("A cited reference.", 160),
            ),
        ),
        ReconstructedPage(
            number=3,
            lines=(
                _heading("Annex B: Canonical Form", 100),
                _line("(normative)", 120),
                _heading("B.1 Overview", 140),
                _line("Overview body.", 160),
                _heading("B.2 Constraints", 180),
                _line("Constraints body.", 200),
                _heading("B.2.1 First Constraint", 220),
                _line("First constraint body.", 240),
                _heading("B.3 Ordering", 260),
                _line("Ordering body.", 280),
            ),
        ),
    ]

    clauses = detect_clauses(pages)

    assert [clause.number for clause in clauses] == ["10.3", "10.4", "A", "B", "B.1", "B.2", "B.2.1", "B.3"]
    example, bibliography, canonical = clauses[1], clauses[2], clauses[3]
    assert [line.text for line in example.raw_lines] == ["Example body."]  # no annex text folded in
    assert (example.page_start, example.page_end) == (1, 1)
    assert bibliography.title == "Bibliography"
    assert [line.text for line in bibliography.raw_lines] == ["A cited reference."]
    assert canonical.title == "Canonical Form"
    assert canonical.file_name == "B-canonical-form.md"
    assert clauses[6].file_name == "B.2.1-first-constraint.md"


def test_detect_clauses_takes_each_annex_designation_as_its_clauses_normative_flag() -> None:
    pages = [
        ReconstructedPage(
            number=1,
            lines=(
                _heading("1 Scope", 100),
                _line("Implementations shall conform.", 120),
                _heading("Annex A: Diagrams", 140),
                _line("(informative)", 160),
                _line("Tools shall draw diagrams, but this annex is informative.", 180),
                _heading("A.1 Shapes", 200),
                _line("Shape body.", 220),
                _heading("Annex B: Interchange", 240),
                _line("(Normative)", 260),
                _heading("B.1 Summary", 280),
                _line("Summary body without any requirement keyword.", 300),
            ),
        )
    ]

    clauses = detect_clauses(pages)

    assert [(clause.number, clause.annex_status) for clause in clauses] == [
        ("1", None),
        ("A", "informative"),
        ("A.1", "informative"),
        ("B", "normative"),
        ("B.1", "normative"),
    ]
    for clause in clauses:
        split_normative(clause)
    assert [clause.is_normative for clause in clauses] == [True, False, False, True, True]
    assert all("(informative)" not in line.text for line in clauses[1].raw_lines)


def test_detect_clauses_ignores_body_sized_annex_references_and_out_of_order_annex_subclauses() -> None:
    pages = [
        ReconstructedPage(
            number=1,
            lines=(
                _heading("2 Conformance", 100),
                _line("Annex B specifies the canonical form.", 120),
                _heading("B.3 Canonical Schema", 140),
                _line("More conformance body.", 160),
                _line("Even more conformance body.", 180),
            ),
        )
    ]

    clauses = detect_clauses(pages)

    assert [clause.number for clause in clauses] == ["2"]
    assert [line.text for line in clauses[0].raw_lines] == [
        "Annex B specifies the canonical form.",
        "B.3 Canonical Schema",
        "More conformance body.",
        "Even more conformance body.",
    ]


def test_clause_number_key_sorts_annexes_after_every_numbered_clause_in_letter_order() -> None:
    numbers = ["B.10", "A", "22.3", "B.2.1", "9.3.2", "B", "B.2"]

    assert sorted(numbers, key=clause_number_key) == ["9.3.2", "22.3", "A", "B", "B.2", "B.2.1", "B.10"]


def _toc_page(number: int, *entries: str) -> ReconstructedPage:
    lines = [_line("Table of Contents", 50)]
    lines.extend(_line(entry, 80 + index * 20) for index, entry in enumerate(entries))
    return ReconstructedPage(number=number, lines=tuple(lines))


def test_detect_clauses_takes_a_wrapped_heading_title_from_the_table_of_contents() -> None:
    pages = [
        _toc_page(
            1,
            "7 Concepts ..... 1",
            "7.1 How the format handles information from",
            "the authoring tools ..... 1",
            "7.2 Scenarios ..... 2",
        ),
        ReconstructedPage(
            number=2,
            lines=(
                _heading("7 Concepts", 100),
                _line("Concepts body.", 120),
                _heading("7.1 How the format handles information from", 140),
                _heading("the authoring tools", 160),
                _line("Information body.", 180),
            ),
        ),
        ReconstructedPage(number=3, lines=(_heading("7.2 Scenarios", 100), _line("Scenario body.", 120))),
    ]

    clauses = detect_clauses(pages)

    assert [clause.number for clause in clauses] == ["7", "7.1", "7.2"]
    assert clauses[1].title == "How the format handles information from the authoring tools"
    assert [line.text for line in clauses[1].raw_lines] == ["Information body."]
    assert (clauses[2].page_start, clauses[2].page_end) == (3, 3)


def test_detect_clauses_rejects_a_successor_numbered_heading_that_is_not_in_the_table_of_contents() -> None:
    pages = [
        _toc_page(1, "1 Scope ..... 1", "1.1 Overview ..... 1", "2 Terms ..... 2"),
        ReconstructedPage(
            number=2,
            lines=(
                _heading("1 Scope", 100),
                _heading("1.1 Overview", 120),
                _line("Overview body.", 140),
                _heading("1.2 A numbered figure label", 160),  # successor-shaped, heading-sized, not in the TOC
                _line("More overview body.", 180),
                _heading("2 Terms", 200),
                _line("Terms body.", 220),
            ),
        ),
    ]

    clauses = detect_clauses(pages)

    assert [clause.number for clause in clauses] == ["1", "1.1", "2"]
    assert "1.2 A numbered figure label" in [line.text for line in clauses[1].raw_lines]


def test_detect_clauses_rejects_a_table_of_contents_number_whose_title_does_not_match() -> None:
    pages = [
        _toc_page(1, "1 Scope ..... 1", "2 Terms ..... 2"),
        ReconstructedPage(
            number=2,
            lines=(
                _heading("1 Scope", 100),
                _heading("2 apples were weighed", 120),
                _heading("2 Terms", 140),
                _line("Terms body.", 160),
            ),
        ),
    ]

    clauses = detect_clauses(pages)

    assert [(clause.number, clause.title) for clause in clauses] == [("1", "Scope"), ("2", "Terms")]


def test_detect_clauses_accepts_subclauses_deeper_than_the_table_of_contents_lists() -> None:
    pages = [
        _toc_page(
            1, "3 Annex ..... 1", "3.2 Differences ..... 1", "3.2.3 From the old format ..... 1", "3.3 Tags ..... 2"
        ),
        ReconstructedPage(
            number=2,
            lines=(
                _heading("3 Annex", 100),
                _heading("3.2 Differences", 120),
                _heading("3.2.3 From the old format", 140),
                _heading("3.2.3.1 How identifiers are handled and how to deal with", 160),
                _heading("legacy identifiers", 180),
                _line("Identifier body.", 200),
                _heading("3.2.3.2 Other differences", 220),
                _line("Other body.", 240),
                _heading("3.3 Tags", 260),
                _line("Tags body.", 280),
            ),
        ),
    ]

    clauses = detect_clauses(pages)

    assert [clause.number for clause in clauses] == ["3", "3.2", "3.2.3", "3.2.3.1", "3.2.3.2", "3.3"]
    assert clauses[3].title == "How identifiers are handled and how to deal with legacy identifiers"


def test_detect_clauses_and_toc_returns_the_entries_and_skips_front_matter_before_the_first_clause() -> None:
    pages = [
        ReconstructedPage(number=1, lines=(_line("Licence terms of the document.", 100),)),
        _toc_page(2, "1 Scope ..... 1", "2 Terms ..... 2"),
        ReconstructedPage(number=3, lines=(_heading("Preface", 100), _line("About the publisher.", 120))),
        ReconstructedPage(number=4, lines=(_heading("1 Scope", 100), _line("Scope body.", 120))),
    ]

    clauses, entries = detect_clauses_and_toc(pages)

    assert [entry.number for entry in entries] == ["1", "2"]
    assert [clause.number for clause in clauses] == ["1"]
    assert clauses[0].page_start == 4
    assert [line.text for line in clauses[0].raw_lines] == ["Scope body."]


def test_detect_clauses_without_a_table_of_contents_joins_a_same_font_wrapped_heading() -> None:
    pages = [
        ReconstructedPage(
            number=1,
            lines=(
                _heading("1 A heading long enough", 100),
                _heading("to wrap", 120),
                _line("Body.", 140),
                _line("More body.", 160),
                _line("Even more body.", 180),
            ),
        )
    ]

    clauses = detect_clauses(pages)

    assert clauses[0].title == "A heading long enough to wrap"
    assert [line.text for line in clauses[0].raw_lines] == ["Body.", "More body.", "Even more body."]
