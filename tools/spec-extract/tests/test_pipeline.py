import json
import os
from pathlib import Path

import pytest

from spec_extract.__main__ import main
from spec_extract.pipeline import extract_document
from tests.pdf_fixtures import Page, Text, build_pdf, build_simple_pdf


def test_extract_document_writes_clause_files_and_indexes(tmp_path: Path) -> None:
    pdf_path = tmp_path / "sample.pdf"
    pdf_path.write_bytes(
        build_simple_pdf(
            [
                "1 Scope",
                "This document shall define scope.",
                "It has more than one sentence.",
                "1.1 Overview",
                "This is the overview text.",
                "It also has more than one sentence.",
            ],
            start_y=700,
            line_height=20,
            sizes=[14.0, 10.0, 10.0, 14.0, 10.0, 10.0],
        )
    )
    output_dir = tmp_path / "spec"

    result = extract_document(pdf_path, output_dir, document="ReqIF", version="1.2")

    assert [clause.number for clause in result.clauses] == ["1", "1.1"]

    scope_file = output_dir / "clauses" / "1-scope.md"
    overview_file = output_dir / "clauses" / "1.1-overview.md"
    assert scope_file.exists()
    assert overview_file.exists()
    assert "shall" in scope_file.read_text(encoding="utf-8")

    index = json.loads((output_dir / "index.json").read_text(encoding="utf-8"))
    assert [row["clause"] for row in index] == ["1", "1.1"]
    assert index[0]["normative"] is True
    assert index[1]["normative"] is False
    assert all(row["document"] == "ReqIF" for row in index)
    assert all(row["license"] == "OMG-specification" and row["redistributable"] is False for row in index)
    assert set(index[0]) == {"clause", "title", "document", "pages", "normative", "file", "license", "redistributable"}

    index_md = (output_dir / "index.md").read_text(encoding="utf-8")
    assert "| Document |" in index_md
    assert "| License | Redistributable |" in index_md
    assert b"\r\n" not in (output_dir / "index.json").read_bytes()


def test_extract_document_threads_the_document_name_into_front_matter_and_index(tmp_path: Path) -> None:
    """Two documents extracted into sibling knowledge trees (e.g. the ReqIF specification and the
    implementation guide) must never produce byte-shape-identical rows an agent could ground an
    answer in the wrong corpus from."""
    pdf_path = tmp_path / "sample.pdf"
    pdf_path.write_bytes(
        build_simple_pdf(
            ["1 Scope", "This document shall define scope.", "It has more than one sentence."],
            start_y=700,
            line_height=20,
            sizes=[14.0, 10.0, 10.0],
        )
    )
    output_dir = tmp_path / "guide"

    extract_document(pdf_path, output_dir, document="PSI-Guide", version="1.7")

    index = json.loads((output_dir / "index.json").read_text(encoding="utf-8"))
    assert index[0]["document"] == "PSI-Guide"
    assert index[0]["license"] == "prostep-ivip-restricted"
    assert index[0]["redistributable"] is False
    assert index[0]["normative"] is False  # the guide is informative, "shall" or not

    clause_markdown = (output_dir / "clauses" / "1-scope.md").read_text(encoding="utf-8")
    assert 'document: "PSI-Guide"' in clause_markdown
    assert 'version: "1.7"' in clause_markdown
    assert 'license: "prostep-ivip-restricted"' in clause_markdown
    assert "redistributable: false" in clause_markdown


def test_extract_document_records_an_unknown_license_for_a_document_without_a_profile(tmp_path: Path) -> None:
    pdf_path = tmp_path / "sample.pdf"
    pdf_path.write_bytes(build_simple_pdf(["1 Scope", "Body one.", "Body two."], sizes=[14.0, 10.0, 10.0]))

    result = extract_document(pdf_path, tmp_path / "other", document="Other", version="1")

    index = json.loads((result.output_dir / "index.json").read_text(encoding="utf-8"))
    assert (index[0]["license"], index[0]["redistributable"]) == ("unknown", False)


def _guide_page(number: int, body: list[Text]) -> Page:
    """A synthetic page with an implementation-guide-like running header and footer."""
    header_and_footer = [
        Text("Example Guide", 792 - 48, size=10),
        Text("Version 2.0, 1 January 2030", 792 - 71, size=10),
        Text(f"Relating to Thing 4.2 {number}", 52, size=10),
        Text("Example Association All rights kept", 40, size=10),
    ]
    return Page(items=header_and_footer + body)


def test_extract_document_on_a_guide_like_pdf_skips_front_matter_and_toc_and_strips_running_text(
    tmp_path: Path,
) -> None:
    def body(*lines: tuple[str, float]) -> list[Text]:
        return [Text(text, 700 - index * 18, size=size) for index, (text, size) in enumerate(lines)]

    pages = [
        _guide_page(1, body(("Abstract", 16), ("Front matter text that is not a clause.", 10))),
        _guide_page(
            2,
            body(
                ("Contents", 16),
                ("1 Introduction ........................ 3", 10),
                ("1.1 Motivation ........................ 3", 10),
                ("2 Rules ............................... 4", 10),
            ),
        ),
        _guide_page(
            3,
            body(
                ("1 Introduction", 16),
                ("1.1 Motivation", 14),
                ("This is an informal document.", 10),
                ("It explains things.", 10),
                ("And more things.", 10),
            ),
        ),
        _guide_page(
            4,
            body(
                ("2 Rules", 16),
                ("Tools should follow this rule.", 10),
                ("They also must do this.", 10),
                ("Plain text.", 10),
            ),
        ),
    ]
    pdf_path = tmp_path / "guide.pdf"
    pdf_path.write_bytes(build_pdf(pages))

    result = extract_document(pdf_path, tmp_path / "out", document="PSI-Guide", version="1.7")

    assert [(clause.number, clause.page_start, clause.page_end) for clause in result.clauses] == [
        ("1", 3, 3),
        ("1.1", 3, 3),
        ("2", 4, 4),
    ]
    assert result.missing_toc_entries == []
    all_text = "\n".join(path.read_text(encoding="utf-8") for path in (tmp_path / "out" / "clauses").glob("*.md"))
    for running_text in (
        "Example Guide",
        "Version 2.0",
        "Relating to Thing",
        "All rights kept",
        "Front matter",
        "....",
    ):
        assert running_text not in all_text
    rules = (tmp_path / "out" / "clauses" / "2-rules.md").read_text(encoding="utf-8")
    assert "normative: false" in rules
    assert "<!-- recommendation -->" in rules


def test_extract_document_removes_clause_files_left_by_a_previous_run(tmp_path: Path) -> None:
    pdf_path = tmp_path / "sample.pdf"
    pdf_path.write_bytes(build_simple_pdf(["1 Scope", "Body one.", "Body two."], sizes=[14.0, 10.0, 10.0]))
    stale = tmp_path / "out" / "clauses" / "1-old-title.md"
    stale.parent.mkdir(parents=True)
    stale.write_text("stale", encoding="utf-8")

    extract_document(pdf_path, tmp_path / "out")

    assert not stale.exists()
    assert (tmp_path / "out" / "clauses" / "1-scope.md").exists()


def test_main_defaults_to_the_reqif_document_and_version(tmp_path: Path) -> None:
    pdf_path = tmp_path / "sample.pdf"
    pdf_path.write_bytes(build_simple_pdf(["1 Scope", "Body one.", "Body two."], sizes=[14.0, 10.0, 10.0]))

    assert main(["extract", "--pdf", str(pdf_path), "--out", str(tmp_path / "out")]) == 0

    clause_markdown = (tmp_path / "out" / "clauses" / "1-scope.md").read_text(encoding="utf-8")
    assert 'document: "ReqIF"' in clause_markdown
    assert 'version: "1.2"' in clause_markdown


# The live tests below run against real, locally-supplied PDFs only. They never fetch a PDF, write
# their output only to pytest's temporary directory (never into the repository), and assert only
# structural properties - clause numbers, counts, page ranges and metadata - never verbatim text,
# which would itself be a redistribution risk in test history or CI artifacts.


def _live_pdf(variable: str) -> str:
    pdf_path = os.environ.get(variable)
    if not pdf_path:
        pytest.skip(f"{variable} not set - this test only runs against a locally-supplied real PDF")
    return pdf_path


@pytest.mark.live
def test_extract_real_reqif_specification_finds_every_table_of_contents_clause(tmp_path: Path) -> None:
    result = extract_document(_live_pdf("REQIF_SPEC_PDF_PATH"), tmp_path / "reqif", document="ReqIF", version="1.2")

    numbers = [clause.number for clause in result.clauses]
    assert len(result.toc_entries) > 90
    assert result.missing_toc_entries == []
    assert len(numbers) == len(set(numbers))
    assert {"10.8", "10.8.1", "10.8.40", "10.8.45", "11.4"} <= set(numbers)
    by_number = {clause.number: clause for clause in result.clauses}
    for entry in result.toc_entries:
        assert by_number[entry.number].title == entry.title
    assert all(clause.page_start >= 11 for clause in result.clauses)  # nothing from the front matter or TOC

    index = json.loads((result.output_dir / "index.json").read_text(encoding="utf-8"))
    assert all(row["license"] == "OMG-specification" and row["redistributable"] is False for row in index)


@pytest.mark.live
def test_extract_real_implementation_guide_finds_every_table_of_contents_clause(tmp_path: Path) -> None:
    result = extract_document(_live_pdf("PSI_GUIDE_PDF_PATH"), tmp_path / "psi", document="PSI-Guide", version="1.7")

    numbers = [clause.number for clause in result.clauses]
    assert len(result.toc_entries) >= 40
    assert result.missing_toc_entries == []
    assert len(numbers) == len(set(numbers))
    assert {"1", "2.19", "3.3"} <= set(numbers)
    assert all(clause.page_start >= 7 for clause in result.clauses)  # nothing from the front matter or TOC
    assert not any(clause.is_normative for clause in result.clauses)

    index = json.loads((result.output_dir / "index.json").read_text(encoding="utf-8"))
    assert all(row["license"] == "prostep-ivip-restricted" and row["redistributable"] is False for row in index)
