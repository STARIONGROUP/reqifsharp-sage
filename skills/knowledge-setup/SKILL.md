---
name: knowledge-setup
description: Check whether the ReqIF knowledge base (OMG ReqIF 1.2 metamodel, schema and spec text, the optional prostep ivip Implementation Guide, and the reqifsharp API reference) is set up, and fetch/import/generate it. Use when another reqifsharp-sage skill reports the knowledge base is missing or stale, when the user asks to set up, update or refresh the ReqIF knowledge, wants to import their copy of the prostep Implementation Guide, or when a SessionStart message from reqifsharp-sage says something needs setting up.
---

# Knowledge setup

The knowledge base is built on the user's machine; nothing OMG- or prostep-derived ships with
the plugin (see `NOTICE`). Everything lives under `<plugin-root>/sources/` and
`<plugin-root>/knowledge/`, both git-ignored.

## Finding the CLI and the plugin root

The CLI (`reqifsharp-sage`) is **not on PATH**. The SessionStart context from reqifsharp-sage
states the plugin root and the exact CLI command to run; use those. If that context is missing:

- `<plugin-root>` is two directories above this skill's base directory.
- A locally built CLI may exist at `<plugin-root>/tools/sage-cli/ReqIFSharp.Sage.Tools/bin/{Release,Debug}/net10.0/reqifsharp-sage[.exe]`.
- Otherwise, with the .NET 10 SDK installed:
  `dotnet run --project <plugin-root>/tools/sage-cli/ReqIFSharp.Sage.Tools -- <verb> ...`

Always pass `--repository-root "<plugin-root>"`. Below, `<cli>` means that command.

## 1. Check status (offline, safe to run any time)

```bash
<cli> check --json --repository-root "<plugin-root>"
```

The output has `default`, `versions[]` (`fetched`, `generated`, `specGenerated`), `guides[]`
(`imported`, `generated`), `api` (`reqIFSharpVersion`), `apiUpToDate` and human-readable `summary`
lines. `<cli> versions --json` lists everything the CLI knows how to fetch.

## 2. Fetch (network or local files): ask the user first

```bash
<cli> fetch --version 1.2 --repository-root "<plugin-root>"
```

This downloads the ReqIF 1.2 specification PDF, `reqif.cmof`, `reqif.xsd` and `driver.xsd` from
omg.org. Options:

- `--from <dir>` takes the files from a local folder instead. A file is recognised by its SHA-256
  (so `formal-16-07-01.pdf` and `dtc-10-12-13.cmof.xml` are found), then by name. Missing files are
  still downloaded.
- `--no-specs` skips the PDF: metamodel, schema and API lookups still work, and spec citation
  falls back to clause numbers.

## 3. Import the prostep ivip Implementation Guide (optional, the user's own copy)

The guide is **not public**. Never download it, never ask the user to share it, and never commit
it. If the user has a copy:

```bash
<cli> import-guide "<path-to-PSI_ImplementationGuide_ReqIF_V1-7.pdf>" --repository-root "<plugin-root>"
```

A known release (v1.7) is recognised by its hash. For any other file, add `--version <x.y>`.

## 4. Generate

```bash
<cli> generate --version 1.2 --repository-root "<plugin-root>"
```

This builds the metamodel and schema pages, extracts the spec text and the text of every imported
guide (Python via `tools/spec-extract`, or `uv` provisioned on demand), and generates the reqifsharp
API reference for the ReqIFSharp version the CLI bundles. Each step reports success or a skip
reason. A skipped text extraction never blocks the rest.

If the text extraction is skipped because `pdfplumber` is missing and `uv` could not be provisioned,
offer to run `pip install -e "<plugin-root>/tools/spec-extract"`. Do not run it unasked.

## Other verbs

- `<cli> use --version <v>` switches the default ReqIF version.
- `<cli> remove --version <v> [--force]` deletes one ReqIF version's sources and knowledge.
- `<cli> remove --guide <v>` deletes an imported guide and its extracted text.

## Reporting

Report what is ready and what is not, per part: metamodel, schema, spec text, guide text, API.
Name the next command to run for anything missing.
