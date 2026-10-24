---
name: reqifsharp-usage
description: How to use and develop the reqifsharp C# library (NuGet ReqIFSharp and ReqIFSharp.Extensions) for OMG ReqIF files - reading and writing .reqif/.reqifz, building documents in code, attribute values, XHTML, attachments, validation, and its known gotchas. Use when the user writes or reviews C# that reads or writes ReqIF with reqifsharp, asks how to do something with it ("read a reqifz", "create a SpecObject", "add an enumeration value", "walk the SpecHierarchy", "get the plain text of an XHTML attribute", "validate against the schema", "use ReqIFLoaderService", "extract the images"), hits an exception or unexpected result from ReqIFDeserializer/ReqIFSerializer, or contributes to the reqifsharp repository ("add a property to reqifsharp", "implement ReadXml/WriteXml", "write a test fixture").
---

# reqifsharp usage

Practical guidance for the **reqifsharp** library, verified against **ReqIFSharp 10.1.2** and
**ReqIFSharp.Extensions 5.2.2** (both `netstandard2.0`). The examples in `reference/` were
compiled and run against those packages: they write a `.reqif` and a `.reqifz`, read them back,
and validate them with zero schema errors.

Paths below are relative to this skill's base directory.

## Which file to read

| Question | Read |
|---|---|
| Deserialize from a path or stream, `.reqifz` with several documents, attachments, `validate: true`, logging, navigating `TheHeader`/`CoreContent`, typed attribute values, enumeration literals, XHTML markup vs plain text, walking `SpecHierarchy` trees, trace links | `reference/reading.md` |
| Build a document from scratch (header, datatypes, spec types, attribute definitions, spec objects and values, specifications, relations, relation groups, tool extensions), what constructors do, identifiers and `LAST-CHANGE`, XHTML values, `Serialize`/`SerializeAsync`, adding attachments, round-tripping | `reference/writing.md` |
| ReqIFSharp.Extensions: every `Query*` extension method, `ReqIFLoaderService`, `QueryDataAsync`, data URIs, Base64 helpers | `reference/extensions.md` |
| Errors and surprises: placeholders for unresolved references, duplicate identifiers, null vs empty, XHTML namespace, stream disposal, warnings vs errors, cancellation, thread safety, netstandard2.0, breaking changes in 9.x/10.x, bugs not yet fixed | `reference/pitfalls.md` |
| Changing reqifsharp itself: layout, conventions, the `ReadXml`/`WriteXml(Async)` pattern, adding a property end to end, tests, CHANGELOG, CLA, branches, CI, license header | `reference/contributing.md` |

Read only the file the question needs. For an error message, search `pitfalls.md` first.

## Version check first

Find the ReqIFSharp version the user's project references (`PackageReference`, or
`Directory.Packages.props`). The reqifsharp wiki and the `development` branch document builder
methods (`ReqIF.Create`, `content.CreateSpecObject`, `CreateAttributeValue`, `AddChild`,
`Identifiable.CreateIdentifier`, `MarkAsChanged`, `ExtractNormalizedTextFromValue`) that are
**not** in 10.1.2. For 10.1.2 code, build with `new` and add each element to its owner's list
yourself (`writing.md`). Inside the reqifsharp repository the source is the truth.

## Related skills

- `reqifsharp-api-lookup`: exact signatures, constructors, properties and the C# ↔ ReqIF mapping
  from the generated reference at `<plugin-root>/knowledge/reqifsharp/<version>/`.
- `metamodel-lookup`: ReqIF metaclasses, multiplicities, containment, and how each feature is
  serialized (XML element and attribute names, wrappers, `-REF`s). Use it before adding a
  property to reqifsharp.
- `reqif-schema-lookup`: raw XSD types, e.g. the allowed XHTML subset.
- `reqif-spec-citation`: normative wording of the OMG ReqIF specification, with clause numbers.
- `implementation-guide-citation`: the prostep ivip ReqIF Implementation Guide (attribute names
  such as `ReqIF.Text`, tool interoperability rules), if the user imported it.
- `reqif-file-inspection`: check a user's `.reqif`/`.reqifz` file against the schema, metamodel
  and guide rules, beyond what `validate: true` reports.

## Answering

- Tag statements **API** when they come from the library (its source, the generated reference
  or a run), with the package version ("ReqIFSharp 10.1.2"), and **DERIVED** when they are
  guidance, workarounds or conclusions of your own.
- Before giving the user code, check every type, member and signature you use against
  `reqifsharp-api-lookup` for their version; do not invent overloads. Prefer the patterns in the
  reference files, which compile.
- Show complete snippets with their `using` directives (`ReqIFSharp`;
  `ReqIFSharp.Extensions.ReqIFExtensions` for the query methods; `ReqIFSharp.Extensions.Services`
  for the loader service), and follow reqifsharp naming (`TheHeader`, `CoreContent`,
  `SpecObjects`, `TheValue`, `DataTpeDefEnum`, `SpecHierarchy.ReqIfContent`).
- When code writes ReqIF, recommend reading the result back with `validate: true` and counting
  only `XmlSeverityType.Error` findings.
- Paraphrase the OMG specification and the Implementation Guide; quote them only through the
  citation skills.
