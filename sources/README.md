# sources/

These are the raw input files of the knowledge base, stored by `reqifsharp-sage fetch` and
`reqifsharp-sage import-guide`. **Nothing under `sources/` is ever committed**; only this README
is. See the root `CLAUDE.md` ("Committed vs git-ignored") and `NOTICE`.

## ReqIF 1.2 (`sources/1.2/`)

| File | Fetched from | Notes |
|---|---|---|
| `specs/ReqIF-1.2.pdf` | https://www.omg.org/spec/ReqIF/1.2/PDF | formal/16-07-01; skipped by `fetch --no-specs` |
| `cmof/reqif.cmof` | https://www.omg.org/spec/ReqIF/20101201/reqif.cmof | normative metamodel; also published as `dtc/10-12-13` |
| `schema/reqif.xsd` | https://www.omg.org/spec/ReqIF/20110401/reqif.xsd | target namespace `http://www.omg.org/spec/ReqIF/20110401/reqif.xsd`, shared by ReqIF 1.0.1–1.2 |
| `schema/driver.xsd` | https://www.omg.org/spec/ReqIF/20110402/driver.xsd | the XHTML 1.1 subset allowed in `XHTML-CONTENT` |
| `fetch-manifest.json` | – | the URL, SHA-256, origin (`download` or `local`) and whether it matches the published file |

These URLs are the machine-consumable files listed on the specification's cover page. The
expected SHA-256 of each file is registered in `KnownReqIFVersions`. That is how `fetch --from <dir>`
recognises local copies whatever their names (e.g. `formal-16-07-01.pdf`,
`dtc-10-12-13.cmof.xml`), and how `fetch-manifest.json` flags a copy that differs from the published
file.

The XHTML Modularization module schemas that `driver.xsd` includes (hosted by W3C) are not fetched.
The knowledge base only needs `driver.xsd` itself, and `inspect` validates with the schemas
embedded in ReqIFSharp.

License: the OMG specification license; the terms are reproduced in `NOTICE`. They permit
informational use but not redistribution, which is why these files are fetched per machine.

## prostep ivip ReqIF Implementation Guide (`sources/psi-guide/<version>/`)

| File | Origin | Notes |
|---|---|---|
| `PSI-ReqIF-Implementation-Guide-<version>.pdf` | **your own copy**, via `import-guide <pdf>` | v1.7 (2019-12-16) is recognised by its SHA-256 |
| `import-manifest.json` | – | version, SHA-256, known release or not, import time |

The guide is copyright prostep ivip Association / VDA and **is not publicly available**. The CLI
never downloads it. Its PDF and the text extracted from it (`knowledge/psi-guide/`) stay on your
machine and must not be committed, published or shared.

## Test fixtures

Test fixtures under `tools/**/Fixtures/` are hand-written, fictional metamodels, schemas and ReqIF
files. No OMG files, prostep text or third-party sample files (such as ProR's) are committed as
test data.
