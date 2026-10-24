# SPDX-FileCopyrightText: 2026 Starion Group S.A.
# SPDX-License-Identifier: Apache-2.0

"""Stage 4: group a clause's raw lines into paragraphs and split normative from informative.

Paragraphs are separated by a vertical gap visibly larger than the document's line spacing, and
also start at a NOTE/EXAMPLE prefix, a bullet, a run-in bold label ("Package: ReqIF"), after a
one-line bold label ("Attributes"), on a new page after a finished sentence, around a ruled table
(rendered as a markdown table) and around monospaced lines (rendered as a fenced code block, line
breaks kept).

A paragraph is normative when it contains one of the profile's normative keywords (by default
"shall"/"must" and the upper-case RFC 2119 keywords) and is not a NOTE or EXAMPLE; an informative
document's profile has no normative keywords at all. A profile may also give recommendation
keywords, for documents (such as an implementation guide) whose advice is worth marking although it
is not normative.
"""

from __future__ import annotations

import re
from collections import Counter

from spec_extract.models import Clause, Line, Paragraph
from spec_extract.profiles import GENERIC_PROFILE

_NOTE_PREFIX = re.compile(r"^NOTE\b")
_EXAMPLE_PREFIX = re.compile(r"^EXAMPLE\b")
_BULLET = re.compile(r"^[\u2022\u25cf\u25aa\u2023\u2043\uf0b7\uf0a7\u00b7]\s*")
_RUN_IN_LABEL = re.compile(r"^[A-Za-z][\w .\-]{0,30}:(?:\s|$)")
_GAP_MULTIPLIER = 1.3
_FALLBACK_GAP_PERCENTILE = 0.25
_MIN_SPACING_SHARE = 0.1
_SENTENCE_ENDS = (".", ":", "?", "!")
_CODE_GAP_MULTIPLIER = 2.5
_MONOSPACE_CHAR_WIDTH = 0.6
_MIN_SPACING_FONT_FRACTION = 0.9


def _typical_gap(lines: list[Line]) -> float:
    """A low percentile of the clause's own line gaps - its line spacing when no document-wide one is known."""
    gaps = sorted(second.top - first.top for first, second in zip(lines, lines[1:]) if second.top > first.top)
    return gaps[int(len(gaps) * _FALLBACK_GAP_PERCENTILE)] if gaps else 0.0


def document_line_spacing(pages: list[list[Line]], body_font_size: float) -> float:
    """The document's body line spacing: the smallest gap between consecutive body-text lines (on the
    same page) that is common - at least `_MIN_SPACING_SHARE` of all such gaps. The most common gap
    is not used, since a document with many one-line paragraphs has more paragraph gaps than line gaps."""
    counts: Counter[int] = Counter()
    for lines in pages:
        for first, second in zip(lines, lines[1:]):
            gap = second.top - first.top
            if (
                gap >= body_font_size * _MIN_SPACING_FONT_FRACTION
                and _is_body_line(first, body_font_size)
                and _is_body_line(second, body_font_size)
            ):
                counts[round(gap)] += 1
    total = sum(counts.values())
    return float(next((gap for gap in sorted(counts) if counts[gap] >= total * _MIN_SPACING_SHARE), 0))


def _is_body_line(line: Line, body_font_size: float) -> bool:
    sizes = [word.size for word in line.words]
    return bool(sizes) and abs(max(sizes) - body_font_size) < 0.5


def _has_run_in_label(line: Line) -> bool:
    """Whether `line` starts with a bold "Label:" ("Package: ReqIF", "Figure 1: ...")."""
    return bool(line.words) and "bold" in line.words[0].font_name.lower() and bool(_RUN_IN_LABEL.match(line.text))


def _starts_paragraph(current: list[Line], line: Line, threshold: float) -> bool:
    previous = current[-1]
    gap = line.top - previous.top
    text = line.text
    code = line.is_monospace and previous.is_monospace
    gap_threshold = threshold * _CODE_GAP_MULTIPLIER if code else threshold
    return bool(
        (gap_threshold > 0 and gap > gap_threshold)
        or (gap < 0 and previous.text.rstrip().endswith(_SENTENCE_ENDS))  # a new page after a finished sentence
        or previous.table is not None
        or line.table is not None
        or _NOTE_PREFIX.match(text)
        or _EXAMPLE_PREFIX.match(text)
        or _BULLET.match(text)
        or _has_run_in_label(line)
        or line.is_monospace != previous.is_monospace
        or (not line.is_bold and all(earlier.is_bold for earlier in current))  # after a bold label
    )


def _group_paragraphs(lines: list[Line], line_spacing: float | None) -> list[list[Line]]:
    if not lines:
        return []

    spacing = line_spacing if line_spacing else _typical_gap(lines)
    threshold = spacing * _GAP_MULTIPLIER

    paragraphs: list[list[Line]] = [[lines[0]]]
    for line in lines[1:]:
        if (continued := _continued_table(paragraphs[-1], line)) is not None:
            paragraphs[-1] = [continued]
        elif _starts_paragraph(paragraphs[-1], line, threshold):
            paragraphs.append([line])
        else:
            paragraphs[-1].append(line)
    return paragraphs


def _continued_table(current: list[Line], line: Line) -> Line | None:
    """`current`'s table extended by `line`'s rows, when `line` is the same table continued at the top
    of the next page (same column count, nothing in between); a repeated header row is dropped."""
    previous = current[-1]
    if len(current) != 1 or previous.table is None or line.table is None or line.top > previous.top:
        return None
    if len(previous.table[0]) != len(line.table[0]):
        return None
    rows = line.table[1:] if line.table[0] == previous.table[0] else line.table
    return Line(top=line.top, words=(), table=previous.table + rows)


def _join_lines(lines: list[Line]) -> str:
    """Join a paragraph's lines, without a space after a line-ending hyphen ("non-" "proprietary")."""
    text = ""
    for line in lines:
        part = " ".join(line.text.split())
        if not part:
            continue
        if not text:
            text = part
        elif text.endswith("-") and len(text) > 1 and text[-2].isalnum():
            text += part
        else:
            text = f"{text} {part}"
    return text


def _escape_cell(cell: str) -> str:
    return cell.replace("|", "\\|")


def render_table(rows: tuple[tuple[str, ...], ...]) -> str:
    """Render table rows as a markdown table, the first row as its header."""
    width = max(len(row) for row in rows)
    padded = [tuple(row) + ("",) * (width - len(row)) for row in rows]
    lines = ["| " + " | ".join(_escape_cell(cell) for cell in padded[0]) + " |", "|" + "---|" * width]
    lines.extend("| " + " | ".join(_escape_cell(cell) for cell in row) + " |" for row in padded[1:])
    return "\n".join(lines)


def _render_code(group: list[Line]) -> str:
    """Render monospaced lines as a fenced code block, keeping their relative indentation."""
    left = min(line.words[0].x0 for line in group)
    rendered = []
    for line in group:
        char_width = line.words[0].size * _MONOSPACE_CHAR_WIDTH or 1.0
        indent = max(0, round((line.words[0].x0 - left) / char_width))
        rendered.append(" " * indent + " ".join(line.text.split()))
    code = "\n".join(rendered)
    return f"```\n{code}\n```"


def _render(group: list[Line]) -> str:
    if len(group) == 1 and group[0].table is not None:
        return render_table(group[0].table)
    if all(line.is_monospace for line in group):
        return _render_code(group)
    text = _join_lines(group)
    text = _BULLET.sub("- ", text, count=1)
    if all(line.is_bold for line in group) and text:
        return f"**{text}**"
    if _has_run_in_label(group[0]) and not group[0].is_bold:
        bold_words = []
        for word in group[0].words:
            if "bold" not in word.font_name.lower():
                break
            bold_words.append(word.text)
        label = " ".join(bold_words)
        return f"**{label}**{text[len(label) :]}"
    return text


def _classify(
    text: str, normative_pattern: re.Pattern[str] | None, recommendation_pattern: re.Pattern[str] | None
) -> Paragraph:
    informative_kind = "note" if _NOTE_PREFIX.match(text) else "example" if _EXAMPLE_PREFIX.match(text) else None
    is_normative = informative_kind is None and normative_pattern is not None and bool(normative_pattern.search(text))
    is_recommendation = (
        informative_kind is None and recommendation_pattern is not None and bool(recommendation_pattern.search(text))
    )
    return Paragraph(
        text=text, is_normative=is_normative, informative_kind=informative_kind, is_recommendation=is_recommendation
    )


def split_normative(
    clause: Clause,
    *,
    line_spacing: float | None = None,
    normative_pattern: re.Pattern[str] | None = GENERIC_PROFILE.normative_pattern,
    recommendation_pattern: re.Pattern[str] | None = None,
) -> Clause:
    """Populate `clause.paragraphs` from `clause.raw_lines`, tagging each as normative/informative.

    `line_spacing` is the document's body line spacing (see `document_line_spacing`); without it the
    clause's own line gaps are used. `normative_pattern` of `None` means no paragraph is normative."""
    for group in _group_paragraphs(clause.raw_lines, line_spacing):
        text = _render(group)
        if text:
            # a table's "Recommended" column is data, not a recommendation of the guide's own
            is_table = group[0].table is not None
            clause.paragraphs.append(_classify(text, normative_pattern, None if is_table else recommendation_pattern))
    return clause
