# SPDX-FileCopyrightText: 2026 Starion Group S.A.
# SPDX-License-Identifier: Apache-2.0

"""Per-document formatting and licensing profiles, keyed by the `--document` name.

The pipeline stages are generic; what differs between the documents this package extracts is kept
here as data: the running header/footer text to strip, where the Table of Contents is, which words
make a paragraph normative, and the licence the extracted text is under.

- **ReqIF** - the OMG Requirements Interchange Format (ReqIF) 1.2 formal specification. Its
  normative text uses "shall"/"must" and the upper-case RFC 2119 keywords ("MUST", "SHOULD",
  "MAY", ...). OMG specification text is copyrighted and must not be redistributed.
- **PSI-Guide** - the prostep ivip "ReqIF Implementation Guide". It describes itself as an
  additional, informal document, so none of its clauses is normative; paragraphs that carry
  recommendation wording are marked instead. Its text is restricted to prostep ivip terms and must
  never be committed or redistributed.

Any other document name gets the generic profile: automatic header/footer and Table of Contents
detection, the OMG-style normative keywords, and an "unknown", non-redistributable licence.
"""

from __future__ import annotations

import re
from dataclasses import dataclass

_OMG_NORMATIVE = re.compile(r"\bshall\b|\bmust\b|\b(?:SHALL|MUST|REQUIRED|SHOULD|RECOMMENDED|MAY|OPTIONAL)\b")
_RECOMMENDATION = re.compile(r"\bshould\b|\bSHOULD\b|\b[Rr]ecommend(?:ed|s|ation)?\b|\bRECOMMENDED\b")


@dataclass(frozen=True)
class DocumentProfile:
    """What the pipeline needs to know about one document's formatting and licence."""

    name: str
    license: str
    redistributable: bool
    strip_patterns: tuple[re.Pattern[str], ...] = ()
    """Running header/footer lines to remove from a page's top and bottom bands, matched against the
    whitespace-normalized line text, in addition to the automatically detected repeated lines."""
    toc_pages: tuple[int, int] | None = None
    """First and last (1-based) PDF page of the Table of Contents, or `None` to detect it."""
    normative_pattern: re.Pattern[str] | None = _OMG_NORMATIVE
    """Words that make a paragraph normative, or `None` for an informative document."""
    recommendation_pattern: re.Pattern[str] | None = None
    """Words that mark a paragraph as a recommendation, or `None` not to mark any."""


GENERIC_PROFILE = DocumentProfile(name="generic", license="unknown", redistributable=False)

_PROFILES = {
    "reqif": DocumentProfile(
        name="ReqIF",
        license="OMG-specification",
        redistributable=False,
        strip_patterns=(
            re.compile(
                r"^(?:[ivxlc]+|\d+)?\s*Requirements Interchange Format \(ReqIF\), v1\.2\s*(?:[ivxlc]+|\d+)?$", re.I
            ),
        ),
        toc_pages=(5, 7),
    ),
    "psi-guide": DocumentProfile(
        name="PSI-Guide",
        license="prostep-ivip-restricted",
        redistributable=False,
        strip_patterns=(
            re.compile(r"^ReqIF Implementation Guide$"),
            re.compile(r"^Version \d+(?:\.\d+)*, \d{1,2} \w+ \d{4}$"),
            re.compile(r"^Referring to ReqIF\s?\d+(?:\.\d+)*\s+\d+$"),
            re.compile(r"^\W*\s*(?:VDA, )?prostep ivip Association\b.*All rights reserved(?:\s+[ivxlc\d]+)?$", re.I),
        ),
        toc_pages=(4, 5),
        normative_pattern=None,
        recommendation_pattern=_RECOMMENDATION,
    ),
}


def profile_for(document: str) -> DocumentProfile:
    """The profile for a `--document` name (case-insensitive), or the generic one if it has none."""
    return _PROFILES.get(document.strip().lower(), GENERIC_PROFILE)
