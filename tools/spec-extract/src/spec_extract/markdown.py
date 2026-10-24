"""Stage 5: render one clause to a markdown file with YAML front matter."""

from __future__ import annotations

from spec_extract.models import Clause


def _yaml_escape(text: str) -> str:
    return text.replace("\\", "\\\\").replace('"', '\\"')


def render_clause_markdown(
    clause: Clause, *, document: str, version: str, license: str = "unknown", redistributable: bool = False
) -> str:
    """Render a single clause as markdown: YAML front matter, heading, then body.

    `license` and `redistributable` record the terms the source document's text is under (see
    `DocumentProfile`), so every extracted file carries them even when copied out of its tree."""
    pages = f"{clause.page_start}" if clause.page_start == clause.page_end else f"{clause.page_start}-{clause.page_end}"

    front_matter = "\n".join(
        [
            "---",
            f'clause: "{clause.number}"',
            f'title: "{_yaml_escape(clause.title)}"',
            f'document: "{_yaml_escape(document)}"',
            f'version: "{_yaml_escape(version)}"',
            f'pages: "{pages}"',
            f"normative: {'true' if clause.is_normative else 'false'}",
            f'license: "{_yaml_escape(license)}"',
            f"redistributable: {'true' if redistributable else 'false'}",
            "---",
        ]
    )

    body_parts = [f"# {clause.heading_label} {clause.title}"]
    for paragraph in clause.paragraphs:
        if paragraph.informative_kind:
            body_parts.append(f"<!-- informative:{paragraph.informative_kind} -->")
            body_parts.append(paragraph.text)
            body_parts.append(f"<!-- /informative:{paragraph.informative_kind} -->")
        elif paragraph.is_recommendation:
            body_parts.append("<!-- recommendation -->")
            body_parts.append(paragraph.text)
            body_parts.append("<!-- /recommendation -->")
        else:
            body_parts.append(paragraph.text)

    return front_matter + "\n\n" + "\n\n".join(body_parts) + "\n"
