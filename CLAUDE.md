# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working in this repository.

## What this repo is

This repository holds two things:

1. **A Claude Code plugin** (`.claude-plugin/`, `skills/`, `agents/`, `commands/`, `hooks/native/`).
   It makes Claude knowledgeable about the OMG ReqIF 1.2 standard, the prostep ivip ReqIF
   Implementation Guide (when the user imports their own copy) and the reqifsharp C# library.
2. **The toolchain** (`tools/`) that builds the plugin's knowledge base on each user's machine, plus
   the `inspect` engine for `.reqif`/`.reqifz` files. None of `tools/` ships with the installed
   plugin; the hook downloads a released CLI (see `hooks/native/README.md`).

It is modelled on the sibling plugins
[uml4net-sage](https://github.com/STARIONGROUP/uml4net-sage), from which it was forked, and
[mycelium-hypha](https://github.com/mycelium-cmbse/mycelium-hypha). PDF extraction stays in Python;
everything else is C#.

## Build & test

```bash
dotnet build reqifsharp-sage.slnx
dotnet test reqifsharp-sage.slnx

cd tools/spec-extract
python -m venv .venv && .venv/Scripts/pip install -e ".[dev]"   # .venv/bin/pip on Linux/macOS
.venv/Scripts/pytest -v
.venv/Scripts/ruff check . && .venv/Scripts/ruff format --check .
```

End-to-end smoke test, from the repo root, with the OMG files in the parent folder:

```bash
CLI=tools/sage-cli/ReqIFSharp.Sage.Tools/bin/Debug/net10.0/reqifsharp-sage.exe
$CLI fetch --from ..        # or without --from to download from omg.org
$CLI import-guide ../PSI_ImplementationGuide_ReqIF_V1-7.pdf
$CLI generate && $CLI check
$CLI inspect some.reqifz --json
```

Tests use NUnit with `Assert.That` and snake_case sentence names. Python tests use pytest. All
tooling targets `net10.0`. ReqIFSharp is pinned in `Directory.Packages.props`; bumping it changes
the generated API reference and the deserializer `inspect` uses.

## Architecture

| Project | Purpose |
|---|---|
| `tools/metamodel-gen/ReqIFSharp.Sage.MetamodelGen` | `CmofReader` (EA-exported CMOF 2.0 via `XDocument`) and `XsdReader` (structural reqif.xsd/driver.xsd walk), joined by `MetamodelView`, which binds every metamodel feature to its XML attribute or wrapper element through `ReqIFXmlNames` (`SpecObject` → `SPEC-OBJECT`, `longName` → `LONG-NAME`). Writes `metamodel/` and `schema/`. |
| `tools/apidoc-gen/ReqIFSharp.Sage.ApiGen` | In-process reflection over the ReqIFSharp/ReqIFSharp.Extensions assemblies plus their XML docs. Reads the library's `ReqIfClass`/`ReqIfProperty` attributes for the ReqIF mapping. Writes `reqifsharp/<version>/`. |
| `tools/knowledge-gen/ReqIFSharp.Sage.Knowledge` | `KnownReqIFVersions` (URLs and SHA-256 per source file), `KnownGuideVersions`, `SourceFetcher` (omg.org or `--from` a local folder, matched by hash), `GuideImporter`, `PythonSpecExtractRunner` (the C#/Python seam, with an on-demand `uv` fallback), `DataPackageGenerator`, `InstalledVersionsStore`, `KnowledgeGenerationService` |
| `tools/sage-cli/ReqIFSharp.Sage.Tools` | The `reqifsharp-sage` CLI: `fetch`, `import-guide`, `generate`, `versions`, `use`, `remove`, `check`, `inspect`. `Inspection/` is the rule engine (XSD + R-xxx metamodel rules + G-xxx guide rules). |
| `tools/sage-cli/ReqIFSharp.Sage.Tools.Hook` | The committed, NativeAOT, dependency-free SessionStart hook. It prefers a locally built CLI, otherwise the cached pinned release, then runs `check --json` and tells Claude the CLI path and plugin root. A missing release is downloaded by a detached `--provision` run of the hook itself (`BackgroundProvisioning`), never within the session's 8-second budget. |
| `tools/spec-extract` | Python/pdfplumber: PDF to clause markdown. Document profiles (`profiles.py`) for `ReqIF` and `PSI-Guide`, with TOC-guided heading detection. |

Points to keep in mind:

- **The CLI is not on PATH.** Skills use the path from the hook's context or the local build, and
  pass `--repository-root`. Without that option, `RepositoryRootResolver` walks up from the current
  directory to find `.claude-plugin/plugin.json`.
- **Features that are not serialized.** Some association ends are not written to the file, such as
  `SpecHierarchy.parent` and `AttributeDefinition.specType`. `MetamodelView.BindingOf` returns
  `none` for them, and pages say "not serialized".
- **Why the CLI is not NativeAOT.** ReqIFSharp, Spectre.Console and System.CommandLine are not
  trim-safe, so the CLI is a self-contained publish (`release.yml`). Only the hook is AOT.
- **Determinism.** Generators sort with `StringComparer.Ordinal` and write LF, UTF-8 without BOM,
  with fixed JSON options (`KnowledgeFileWriter`) and no timestamps or paths. Tests regenerate
  twice and compare bytes.
- **Spec extraction never fails `generate`.** It degrades to `SpecExtractionOutcome.Skipped` with
  a reason.

## Committed vs git-ignored (and why)

This is the most load-bearing convention here.

- **OMG**: the ReqIF specification license permits informational use, not redistribution.
- **prostep ivip**: the Implementation Guide is not public at all.

**Never committed:**

- `sources/<...>/`: the PDFs, CMOF and XSDs, and the imported guide;
- `knowledge/<...>/` and `knowledge/installed.json`;
- any `*.pdf`, `*.cmof`, `*.xsd`, `*.reqif` or `*.reqifz` outside `tools/**/Fixtures/`.

`.gitignore` enforces this.

**Never write Implementation Guide text into the repository**, including code comments, test
data, rule messages, docs, commit messages, issues or PRs. Guide-based inspection rules carry only
clause numbers and our own short descriptions. The same applies to OMG spec text beyond the
clause numbers and titles needed for references. `NOTICE` reproduces OMG's notice as its terms
require.

**No committed test fixtures derived from OMG, prostep or third-party content.** Fixtures are
hand-written toy metamodels, schemas and ReqIF files. It is fine to run tests against the real
files locally in `[Explicit]` tests, or with environment variables (`REQIF_SPEC_PDF_PATH`,
`PSI_GUIDE_PDF_PATH`).

**Committed:** `knowledge/README.md`, `knowledge/registry.json` (kept in sync with the C#
registries by a test), `sources/README.md`, the skills, agents and commands, and the
`hooks/native/*` binaries (built from this repo by `hook-binaries.yml`).

## Conventions

These match the sibling uml4net-sage and reqifsharp repositories:

- 4 spaces, no tabs; no `_` prefix on members; `this.` for instance members.
- `var` unless the type is unclear; C# type aliases.
- Always use braces; no `#region`.
- `using` statements go inside the namespace, `System.*` first.
- XML docs on every public member; one public type per file (small related records may share one).
- Apache 2.0 copyright header on every `.cs` file. Copy it from an existing file.
- Docs use a spaced en dash (–) or a hyphen, not an em dash.
- Branches: the default branch is `development`, with feature branches off it and PRs back into
  it. Never commit directly to `master`. Ask before committing or pushing.

## Do's and don'ts

- Python lives only in `tools/spec-extract`.
- Do not add a network call to `check` or the hook's session path. The hook has an 8-second budget
  and must always exit 0. Downloads belong in the detached `--provision` mode.
- Versions from the command line become directory names. Always go through `RepositoryLayout`,
  whose `ValidateVersion` rejects anything path-like, and check that the version is installed before
  deleting anything.
- When you change `check --json`, update `ReqIFSharp.Sage.Tools.Hook/CheckResult.cs`; the
  property names are a contract.
- When you add or change an inspection rule, update its row in the rule table and the
  `reqif-file-inspection` skill.
