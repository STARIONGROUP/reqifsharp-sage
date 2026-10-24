# knowledge/

This is the knowledge base the skills read. **Only this README and `registry.json` are committed.**
Everything else is generated on your own machine by `reqifsharp-sage fetch` / `import-guide` /
`generate`. It is derived from OMG files and, optionally, from your own copy of the prostep ivip
Implementation Guide, and none of it may be redistributed (see `NOTICE` and the root `CLAUDE.md`).

## Committed

- `registry.json`: the ReqIF and Implementation Guide versions the CLI knows about. It mirrors
  `KnownReqIFVersions` / `KnownGuideVersions`, which a test enforces.

## Generated (git-ignored)

```
knowledge/
├── installed.json                      # local state: default version, versions[], guides[], api
├── <reqif-version>/                    # e.g. 1.2
│   ├── datapackage.json                # Frictionless Data Package over the three index.json files
│   ├── metamodel/
│   │   ├── index.json / index.md       # {name, kind, package, qualifiedName, isAbstract, xmlElement, file}
│   │   ├── metamodel.json              # classes with closures + features with XML bindings (for jq)
│   │   └── elements/<Name>.md          # one page per class / primitive type, with "XML serialization"
│   ├── schema/
│   │   ├── index.json / index.md       # {name, kind, metamodelClass, globalElement, file}
│   │   ├── types/<NAME>.md             # one page per XSD complex/simple type
│   │   └── xhtml-subset.md             # XHTML modules and groups allowed in XHTML-CONTENT
│   └── spec/                           # only when the PDF was fetched and Python/uv is available
│       ├── index.json / index.md       # {clause, title, document, pages, normative, file, license, redistributable}
│       └── clauses/<n>-<slug>.md       # one file per clause, every depth
├── psi-guide/<guide-version>/          # only after `import-guide` (NOT public, never commit)
│   ├── index.json / index.md
│   └── clauses/<n>-<slug>.md           # license: prostep-ivip-restricted
└── reqifsharp/<ReqIFSharp-version>/    # reqifsharp API reference (reflection + XML docs)
    ├── index.json / index.md           # {name, fullName, namespace, assembly, kind, isAbstract, isStatic, reqifClass, xmlElement, file}
    ├── mapping.json                    # ReqIF class ↔ C# type ↔ XML element, with feature ↔ property
    ├── api.json                        # full machine-readable API model
    ├── manifest.json                   # package ids and versions
    └── types/<FullName>.md             # one page per public type
```

All output is deterministic: ordinal sorting, LF line endings, UTF-8 without BOM, and no
timestamps or machine paths. Regenerating gives byte-identical files.

## Provenance tiers

Every skill answer is tagged with where it came from:

| Tier | Source |
|---|---|
| **NORMATIVE** | verbatim text of the OMG ReqIF specification (`spec/`, `normative: true`) |
| **GUIDE** | the prostep ivip Implementation Guide (`psi-guide/`), informative, never overrides the standard |
| **MODEL** | read from the OMG CMOF metamodel or XML schema (`metamodel/`, `schema/`) |
| **API** | read from the reqifsharp packages (`reqifsharp/`) |
| **DERIVED** | inferred or computed by the assistant, e.g. skeletons, advice, cross-references |
