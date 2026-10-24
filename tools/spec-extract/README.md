# spec-extract

PDF-to-markdown clause extraction for two documents:

- the **OMG Requirements Interchange Format (ReqIF) 1.2** formal specification (`--document ReqIF`), and
- the **prostep ivip ReqIF Implementation Guide** v1.7 (`--document PSI-Guide`).

It started as a port of [mycelium-hypha](https://github.com/mycelium-cmbse/mycelium-hypha)'s
`tools/spec-extract`. It is the one Python component in an otherwise all-C# repository; see the
root `CLAUDE.md` for why.

## Pipeline

Pure, independently tested stages, each in its own module under `src/spec_extract/`:

1. **`pdf_reader.py`** wraps [pdfplumber](https://github.com/jsvine/pdfplumber). It extracts positioned
   words (with font name and size) per page, plus ruled tables as whole blocks. Merged-cell spans are
   folded back into logical columns, and ruled diagram boxes that only split labels into letter
   fragments are rejected. No layout logic lives here.
2. **`layout.py`** reconstructs reading order (lines grouped by vertical position, left to right within
   a line) and strips running headers and footers in the top and bottom bands of each page. A line
   counts as running text when it repeats on many pages once a leading or trailing page number is
   ignored, when it is a bare page number, or when it matches one of the profile's `strip_patterns`.
3. **`toc.py`** finds the Table of Contents (from the profile's page range, or the "Contents" page and
   the pages after it that are dense with dotted-leader entries) and parses its numbered entries,
   joining titles that wrap onto a second line.
4. **`clauses.py`** detects clause headings. A heading is a line set in a visibly larger font than
   body text that starts with a clause number or "Annex X". With a Table of Contents, its number must
   be a later entry than the previous heading's and its title must match the entry; the clause takes
   the entry's full title, and the heading's wrapped second line is not kept as body text. Subclauses
   deeper than the Table of Contents lists are accepted when they plausibly follow the previous
   heading. Without a Table of Contents, a successor-numbering heuristic is used instead. Front
   matter, licence pages and the Table of Contents itself never become clauses.
5. **`normative.py`** groups a clause's lines into paragraphs, using the document's line spacing.
   It renders tables (joined across page breaks) as markdown tables, monospaced lines as fenced code
   blocks, bullets as list items and bold run-in labels ("Package:", "Attributes") in bold. It tags
   each paragraph as normative (the profile's keywords: "shall"/"must" and the upper-case RFC 2119
   words for the OMG specification) or informative (`NOTE`/`EXAMPLE`). The implementation guide is
   informative overall, so no paragraph of it is normative; paragraphs with recommendation wording
   ("should", "recommended") are marked instead.
6. **`markdown.py`** renders one clause to markdown with YAML front matter.

`profiles.py` holds what differs between documents, keyed by `--document` (case-insensitive):
running header/footer patterns, Table of Contents pages, normative and recommendation keywords, and
the licence of the extracted text. An unknown document name gets a generic profile (automatic
detection, OMG-style keywords, `license: "unknown"`, `redistributable: false`).

`pipeline.py` runs all stages across a whole PDF and writes:

```
<output_dir>/clauses/<number>-<slug>.md   one file per clause (stale files from an earlier run are removed)
<output_dir>/index.json                   array of {clause, title, document, pages, normative, file,
                                          license, redistributable}, sorted by clause number
<output_dir>/index.md                     human-readable table of the same
```

Each clause file's front matter carries `clause`, `title`, `document`, `version`, `pages`,
`normative`, `license` and `redistributable`. Output is deterministic, UTF-8 with LF line endings.
A Table of Contents entry for which no heading was found is reported on stderr.

| `--document` | `license` | `redistributable` | `normative` |
|---|---|---|---|
| `ReqIF` | `OMG-specification` | `false` | per paragraph keywords |
| `PSI-Guide` | `prostep-ivip-restricted` | `false` | always `false` |
| anything else | `unknown` | `false` | per paragraph keywords |

## Dev workflow

Test-driven, not a distributed CLI, same as hypha's own `spec-extract`. `pytest` is the primary way
to exercise and verify the pipeline:

```bash
python -m venv .venv
.venv/Scripts/pip install -e ".[dev]"   # .venv/bin/pip on Linux/macOS
.venv/Scripts/pytest -v
.venv/Scripts/ruff check .
.venv/Scripts/ruff format .
```

Unit tests use hand-built synthetic data, including small hand-written test PDFs built by
`tests/pdf_fixtures.py`. None of it is derived from the ReqIF specification, the implementation
guide or any other third-party document. Two tests marked `@pytest.mark.live` run the full pipeline
against the real PDFs, but only when `REQIF_SPEC_PDF_PATH` and `PSI_GUIDE_PDF_PATH` point at
locally-supplied copies:

```bash
REQIF_SPEC_PDF_PATH=/path/to/formal-16-07-01.pdf \
PSI_GUIDE_PDF_PATH=/path/to/PSI_ImplementationGuide_ReqIF_V1-7.pdf \
.venv/Scripts/pytest -m live
```

They never fetch a PDF, write only to pytest's temporary directory, and assert only structural
properties (every Table of Contents clause found once, titles, page ranges, licence fields), never
verbatim text.

A thin, non-interactive entry point exists so the `reqifsharp-sage` C# CLI's `generate` verb can
invoke this pipeline as a subprocess:

```bash
python -m spec_extract extract --pdf <path-to-pdf> --out <output-dir> [--document ReqIF] [--version 1.2]
python -m spec_extract extract --pdf <path-to-guide-pdf> --out <output-dir> --document PSI-Guide --version 1.7
```

`PythonSpecExtractRunner` runs this two ways, in order:

1. A `.venv` under this directory (`.venv/Scripts/python.exe` / `.venv/bin/python`) if present, else a
   bare `python`/`python3` on `PATH` - the maintainer dev workflow above.
2. If neither is found (the interpreter itself can't be started), **or** one is found but exits with a
   Python `ModuleNotFoundError` (i.e. `pdfplumber` or `spec_extract` itself isn't installed into it -
   e.g. a bare system Python with nothing `pip install`ed), it provisions [`uv`](https://astral.sh/uv)
   on demand (downloaded and checksum-verified from its GitHub releases, cached under the same local
   app-data folder the CLI's own self-fetch hook uses) and runs `uv run --project tools/spec-extract
   python -m spec_extract ...`. `uv` resolves or fetches a matching Python itself and installs this
   project's dependencies into a managed venv, so verbatim spec citation works for an installed plugin
   too - no pre-existing Python or provisioned `.venv` needed, just network access the first time.
   `uv.lock` (committed) pins that resolution, the same way `Directory.Packages.props` pins exact
   versions on the C# side.
3. If `uv` itself can't be provisioned (offline, or an unsupported platform) or its run also fails,
   `generate` reports the skip with an explicit reason - naming `pdfplumber` specifically when that
   was the cause, with the exact `pip install -e tools/spec-extract` command to fix it - and states
   plainly that the other lookups are unaffected either way; only verbatim spec citation stays
   unavailable until it's resolved.

## Known limitations

Verified against the real ReqIF 1.2 specification (97 clauses, every Table of Contents entry found
once with its title and start page) and the implementation guide (40 Table of Contents clauses plus
two four-level subclauses the contents list does not show). The class descriptions of clause 9.2 and
10.8 come through with their "Package"/"isAbstract"/"Generalization" labels and their
"Attributes"/"Associations"/"Constraints"/"Semantics" sections as separate paragraphs. Remaining rough
edges, all in body text rather than clause numbering, titles or page ranges:

- **Text inside figures.** The ReqIF specification's diagrams are images and contribute no text. The
  implementation guide has one class diagram drawn as vector text, which comes through as a short
  run of jumbled labels next to its caption.
- **Tables.** Only ruled tables are recognised. A table's first row is taken as its header, so a
  two-line header leaves its second line as the first data row. A table continued on the next page is
  joined only when it has the same number of columns.
- **Paragraph breaks at page breaks.** A paragraph that ends exactly at the bottom of a page is
  joined with the first paragraph of the next page unless it ends with sentence punctuation.
- **Bold, non-monospaced code.** XML examples set in a bold proportional font (rather than a
  monospaced one) render as one bold paragraph per line, without indentation.

The citation skills should treat unusually garbled-looking body text with appropriate skepticism.

## Licensing and source document terms

This package never fetches, stores, or commits either source PDF, and its output (the extracted
clause markdown, which is the full verbatim text) is git-ignored - see the root `CLAUDE.md`
("Committed vs git-ignored") and `NOTICE`. Neither document may be redistributed: OMG specification
text is under the OMG's copyright terms, and the implementation guide is restricted to prostep ivip
terms. Every extracted clause file and index row records this in its `license` and `redistributable`
fields.

**PDF library**: [pdfplumber](https://github.com/jsvine/pdfplumber) (MIT) is used instead of
PyMuPDF, which is AGPL-3.0/commercially licensed - an awkward fit even for a non-distributed dev
tool, matching hypha's own reasoning for the same choice.
