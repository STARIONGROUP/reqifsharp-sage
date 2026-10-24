from spec_extract.markdown import render_clause_markdown
from spec_extract.models import Clause, Paragraph


def test_render_clause_markdown_includes_front_matter_and_body() -> None:
    clause = Clause(number="10.8.40", title="Widget", page_start=123, page_end=125)
    clause.paragraphs = [
        Paragraph(text="A Widget shall have a name.", is_normative=True),
        Paragraph(text="NOTE This is informative.", informative_kind="note"),
    ]

    rendered = render_clause_markdown(clause, document="ReqIF", version="1.2")

    assert '---\nclause: "10.8.40"' in rendered
    assert 'title: "Widget"' in rendered
    assert 'document: "ReqIF"' in rendered
    assert 'version: "1.2"' in rendered
    assert 'pages: "123-125"' in rendered
    assert "normative: true" in rendered
    assert "# 10.8.40 Widget" in rendered
    assert "A Widget shall have a name." in rendered
    assert "<!-- informative:note -->" in rendered
    assert "NOTE This is informative." in rendered
    assert "<!-- /informative:note -->" in rendered


def test_render_clause_markdown_single_page_pages_field() -> None:
    clause = Clause(number="1", title="Scope", page_start=3, page_end=3)
    clause.paragraphs = [Paragraph(text="This is scope text.")]

    rendered = render_clause_markdown(clause, document="ReqIF", version="1.2")

    assert 'pages: "3"' in rendered
    assert "normative: false" in rendered


def test_render_clause_markdown_records_the_license_and_redistributable_fields() -> None:
    clause = Clause(number="2.1", title="Naming", page_start=8, page_end=8)
    clause.paragraphs = [Paragraph(text="Files should use this extension.", is_recommendation=True)]

    rendered = render_clause_markdown(
        clause, document="PSI-Guide", version="1.7", license="prostep-ivip-restricted", redistributable=False
    )

    front_matter = rendered.split("---")[1]
    assert 'license: "prostep-ivip-restricted"' in front_matter
    assert "redistributable: false" in front_matter
    assert "<!-- recommendation -->\n\nFiles should use this extension.\n\n<!-- /recommendation -->" in rendered


def test_render_clause_markdown_defaults_to_an_unknown_non_redistributable_license() -> None:
    clause = Clause(number="1", title='A "quoted" title', page_start=1, page_end=1)

    rendered = render_clause_markdown(clause, document="Other", version="1")

    assert 'license: "unknown"' in rendered
    assert "redistributable: false" in rendered
    assert 'title: "A \\"quoted\\" title"' in rendered
