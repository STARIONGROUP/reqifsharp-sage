# reqifsharp-sage

Sage is the AI agent of the [reqifsharp](https://github.com/STARIONGROUP/reqifsharp) ecosystem. It
is a [Claude Code](https://claude.com/claude-code) plugin that gives Claude grounded, citable
knowledge of the **OMG Requirements Interchange Format (ReqIF) 1.2** and of the **reqifsharp** C#
library, to help you develop reqifsharp and build software on top of it.

Answers come from a knowledge base generated on your machine from the authoritative sources:

- the ReqIF **metamodel** (CMOF), joined with the **XML schema**, so every class shows exactly how
  it is written in a `.reqif` file;
- the **specification text**, extracted clause by clause, for verbatim citation;
- the **prostep ivip ReqIF Implementation Guide**, only if you import your own copy (it is not
  public);
- the **reqifsharp API** (ReqIFSharp and ReqIFSharp.Extensions), by reflection over the NuGet
  packages, mapped to ReqIF classes and XML elements.

It also checks `.reqif` and `.reqifz` files: XSD validation, metamodel-level rules (dangling
references, value/definition/type mismatches, enumeration and range violations, ...) and the
Implementation Guide's recommendations.

## Install

```text
/plugin marketplace add STARIONGROUP/reqifsharp-sage
/plugin install reqifsharp-sage@reqifsharp
```

Then ask Claude to "set up the ReqIF knowledge base", or run the CLI yourself. At session start the
plugin tells Claude where the CLI is; it is not on your PATH.

```bash
reqifsharp-sage fetch --version 1.2        # from omg.org; or --from <folder> with local copies
reqifsharp-sage import-guide <guide.pdf>   # optional, your own copy of the prostep guide
reqifsharp-sage generate --version 1.2     # a few seconds
reqifsharp-sage check
```

Extracting the PDF text needs Python with `pdfplumber`. If no suitable Python is found, the CLI
provisions [`uv`](https://astral.sh/uv) on demand. Everything else works without Python.

The Windows SessionStart hook needs Git Bash (Git for Windows). See `hooks/native/README.md`.

## What it knows

| Skill | Ask it |
|---|---|
| `metamodel-lookup` | "What are SpecHierarchy's features and how is it serialized?" |
| `reqif-schema-lookup` | "Which XHTML elements are allowed in THE-VALUE?" |
| `reqif-spec-citation` | "Quote the spec on AttributeDefinitionEnumeration.multiValued." |
| `implementation-guide-citation` | "What does the implementation guide say about ReqIF.ForeignID?" |
| `reqifsharp-api-lookup` | "What overloads does ReqIFDeserializer.DeserializeAsync have?" |
| `reqifsharp-usage` | "How do I write a .reqifz with an attachment?" / "How do I add a property to reqifsharp?" |
| `reqif-file-inspection` | "Check export.reqifz for problems." |
| `knowledge-setup` | "Is the ReqIF knowledge base up to date?" |

Agents for bulk work: `metamodel-navigator`, `spec-citation`, `reqif-inspector`. The slash command
`/reqifsharp-sage:overview` prints a summary.

Every answer is tagged by provenance:

- **NORMATIVE**: verbatim spec text;
- **GUIDE**: Implementation Guide, informative;
- **MODEL**: OMG CMOF/XSD;
- **API**: the reqifsharp packages;
- **DERIVED**: inferred.

## Licensing of the sources

This repository is Apache-2.0. It contains **no** OMG or prostep material:

- **OMG ReqIF 1.2** (formal/16-07-01) and its machine-readable files are downloaded from omg.org to
  your machine. OMG's terms (reproduced in `NOTICE`) allow informational use but not
  redistribution.
- The **prostep ivip ReqIF Implementation Guide** is not publicly available. The plugin never
  downloads it. You import your own copy, and its text stays on your machine.

`sources/`, the generated `knowledge/` and every PDF, CMOF and XSD file are git-ignored. See
`sources/README.md` for the exact URLs and hashes.

## Development

The build, test and architecture notes and the repository's conventions are in `CLAUDE.md` and
`tools/README.md`. In short:

```bash
dotnet build reqifsharp-sage.slnx
dotnet test reqifsharp-sage.slnx
cd tools/spec-extract && .venv/Scripts/pytest && .venv/Scripts/ruff check .
```

Releases: `tools-v<x.y.z>` tags publish the CLI that the hook downloads; `v<x.y.z>` tags are
plugin versions, pinned in `.claude-plugin/marketplace.json`.
