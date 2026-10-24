# tools/

The generation toolchain behind the `reqifsharp-sage` plugin. **Nothing under `tools/` is shipped
with the installed plugin** - the plugin itself only carries `hooks/native/` (the small SessionStart
hook binaries) plus the `skills/`, `agents/`, and `commands/` markdown at the repository root. The
full CLI built here is downloaded on demand by the hook the first time it's actually needed (see
`hooks/native/README.md` and the root `CLAUDE.md`).

| Directory | Responsibility |
|---|---|
| `sage-cli/ReqIFSharp.Sage.Tools` | The `reqifsharp-sage` CLI (`fetch`, `import-guide`, `generate`, `versions`, `use`, `remove`, `check`, `inspect`) and the `inspect` rule engine (`Inspection/`) |
| `sage-cli/ReqIFSharp.Sage.Tools.Hook` | The small, dependency-free NativeAOT SessionStart hook |
| `knowledge-gen/ReqIFSharp.Sage.Knowledge` | Orchestration: the static ReqIF and Implementation Guide registries, source fetching (omg.org or `--from` a local folder), guide import, the Python `spec_extract` subprocess seam (falling back to an on-demand-provisioned `uv` when no Python is found), `datapackage.json` generation, local install-state tracking |
| `metamodel-gen/ReqIFSharp.Sage.MetamodelGen` | Reads the ReqIF CMOF metamodel and XML schemas (plain `XDocument` readers) and generates `metamodel/` and `schema/`, joining every feature to its XML attribute or element |
| `apidoc-gen/ReqIFSharp.Sage.ApiGen` | Reflects over the ReqIFSharp/ReqIFSharp.Extensions packages plus their XML docs and generates the `reqifsharp/<version>/` API reference and the ReqIF ↔ C# ↔ XML `mapping.json` |
| `spec-extract` | Python: PDF-to-markdown clause extraction (the one non-C# component - see its own README for why) |

## Build & test

```bash
dotnet restore reqifsharp-sage.slnx
dotnet build reqifsharp-sage.slnx
dotnet test reqifsharp-sage.slnx

cd tools/spec-extract
python -m venv .venv && .venv/Scripts/pip install -e ".[dev]"   # .venv/bin/pip on Linux/macOS
.venv/Scripts/pytest -v
```

See the root `CLAUDE.md` for coding conventions, the licensing-driven "committed vs git-ignored"
rules, and determinism requirements for generated output.
