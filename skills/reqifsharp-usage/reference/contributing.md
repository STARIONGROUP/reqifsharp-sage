# Contributing to reqifsharp

For work **inside** the reqifsharp repository (https://github.com/STARIONGROUP/reqifsharp). Here
the source on `development` is the truth; the generated API reference describes the released
10.1.2 package, which is older (see "Version state" below). Facts are from the repository's
`CLAUDE.md`, `.github/CONTRIBUTING.md`, the workflows and the source.

## Version state (October 2026)

- Released: ReqIFSharp 10.1.2 / ReqIFSharp.Extensions 5.2.2, tag `10.1.2`.
- `development` is ahead by about ten commits, all under `## [Unreleased]` in `CHANGELOG.md`:
  builder methods (#85), `IsEditableSpecified` (#86), `ExtractNormalizedTextFromValue` (#92),
  namespace-aware reading through the internal `XmlReaderExtensions` (#87, #94, #97),
  `TheOriginalValue` reading (#95), XHTML constructor fixes (#98) and SPDX headers (#103). The
  `<Version>` in the `.csproj` files is still `10.1.2`/`5.2.2` until the release.

## Layout

| Path | Content |
|---|---|
| `ReqIFSharp/` | the model (one folder per family: `AttributeDefinition/`, `AttributeValue/`, `Datatype/`, `SpecElementWithAttributes/`, `SpecType/`, `Decorators/`), `ReqIF.cs`, `ReqIFContent.cs`, `ReqIFHeader.cs`, `ReqIFDeserializer.cs`, `ReqIFSerializer.cs`, `ReqIfFactory.cs` (internal), `ReqIfSchemaResolver.cs`, `Resources/` (embedded `reqif.xsd`, `driver.xsd`, `xml.xsd` and the XHTML module XSDs). Targets `netstandard2.0` |
| `ReqIFSharp.Extensions/` | `ReqIFExtensions/*Extensions.cs` (static query helpers), `Services/` (`IReqIFLoaderService`, `ReqIFLoaderService`), `StringExtensions/`. Targets `netstandard2.0` |
| `ReqIFSharp.Tests/`, `ReqIFSharp.Extensions.Tests/` | NUnit 4 tests, `net10.0`; sample files in `ReqIFSharp.Tests/TestData` |
| `Directory.Build.props` | settings for every project: company, copyright, `LangVersion` 14.0, `Deterministic`, `GenerateDocumentationFile` |
| `Directory.Build.targets` | packaging for the two libraries (metadata, README/LICENSE/NOTICE, SBOM), guarded by `IsPackable` |
| `Directory.Packages.props` | central package versions |
| `global.json` | .NET SDK 10.0.100, `rollForward: latestFeature` |
| `CLA/` | individual and corporate Contributor License Agreements (PDF) |

`InternalsVisibleTo` exposes the internals to both test projects, so tests can call
`ReadXml`/`WriteXml` and the internal constructors directly.

## Build and test

```text
dotnet build ReqIFSharp.sln -c Release
dotnet test ReqIFSharp.sln
dotnet test --filter "FullyQualifiedName~ReqIFDeSerializerTestFixture"
dotnet test ReqIFSharp.sln --settings coverlet.runsettings
```

## Conventions

- Code style (CONTRIBUTING.md and the `.DotSettings` file): 4 spaces; no `_` prefix; `this.` for
  instance members; `var` unless the type is unclear; C# type aliases; braces always; `using`
  directives **inside** the namespace, `System.*` first, then `Microsoft.*`, then the project;
  no `#region`; long descriptive names; XML doc comments on every member (the build generates
  the documentation file).
- File header on `development` (short SPDX form since #103; the 10.1.2 sources still carry the
  long Apache-2.0 notice):

```text
// -------------------------------------------------------------------------------------------------
// <copyright file="MyNewType.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2017-2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------
```

- Logging: classes take an optional `ILoggerFactory` and fall back to `NullLogger<T>.Instance`;
  no other DI. Log skipped elements at `Warning` with line and position, everything else at
  `Trace`.
- Every public type and property that implements a ReqIF metaclass feature carries the metadata
  decorators `[ReqIfClass(name: "SPEC-OBJECT", isAbstract: false)]` and
  `[ReqIfProperty(aggregation: ..., lowerValue: ..., upperValue: ..., isOrdered: ..., isReadOnly: ..., isDerived: ..., isDerivedUnion: ..., isUnique: ..., defaultValue: ...)]`
  (since 10.1.0, #27). Take the values from the metamodel (`metamodel-lookup` skill). The
  reqifsharp-sage API generator reads them to build the C# ↔ ReqIF mapping.
- Constructor triad on model classes: a public parameterless constructor, a public
  `(ILoggerFactory loggerFactory)` constructor, and an `internal` constructor taking the owner
  (`ReqIFContent`, `SpecType`, `SpecElementWithAttributes`, `Specification`/`SpecHierarchy`, ...)
  that sets the back-reference and **adds the new object to the owner's list**. The
  deserializer uses only the internal one.

## The ReadXml / WriteXml pattern

Serialization lives on the model classes; there is no central mapper. Each class has

- `internal void ReadXml(XmlReader reader)` and `internal Task ReadXmlAsync(XmlReader reader, CancellationToken token)`:
  read the XML attributes of the current element, then loop over child elements
  (`reader.ReadSubtree()` for nested content), switch on `LocalName`, log unknown elements;
- `internal void WriteXml(XmlWriter writer)` and `internal Task WriteXmlAsync(XmlWriter writer, CancellationToken token)`:
  the caller has already written the start element; write attributes first, then child
  elements. Throw `SerializationException` when a required reference is null.

Inheritance does the shared parts: `Identifiable` reads and writes `IDENTIFIER`, `LAST-CHANGE`,
`DESC`, `LONG-NAME` and `ALTERNATIVE-ID`; `AccessControlledElement` adds `IS-EDITABLE`;
`SpecElementWithAttributes` reads `TYPE`, `VALUES`, `CHILDREN` and delegates through
`ReadSpecType`, `ReadHierarchy` and `ReadObjectSpecificElements(Async)`; `SpecType` reads
`SPEC-ATTRIBUTES`. On `development`, XML attributes of subclasses go into the virtual
`WriteXmlAttributes(XmlWriter)` / `WriteXmlAttributesAsync(XmlWriter, CancellationToken)` that
`Identifiable.WriteXml` calls **before** `ALTERNATIVE-ID` (the fix for #86; writing an attribute
after a child element throws `InvalidOperationException`).

`ReqIfFactory` (internal, static) is the only place that maps concrete subtypes to and from
element names: `XmlName(AttributeDefinition | DatatypeDefinition | SpecType)`,
`AttributeDefinitionConstruct`, `DatatypeDefinitionConstruct`, `SpecTypeConstruct`. Attribute
values are dispatched in `SpecElementWithAttributes.ReadXml` and `WriteValues`.

Reference resolution happens while reading, by `Identifier` lookup in the `ReqIFContent` lists
read so far (`SingleOrDefault`), so element order matters and duplicates throw.

On `development`, use the internal `XmlReaderExtensions` instead of raw `XmlReader` calls when
you read ReqIF content: `ReadElementContentAsStringToEndElement(Async)` (does not skip the next
sibling in minified XML, #94), `ReadToReqIFDescendant(Async)` (namespace-aware descendant
search, #87), `SkipForeignElement(Async)` and `SkipForeignElementsToEnd(Async)` (skip elements
in another namespace with a warning, #97).

Both variants exist for every operation; keep the sync and async code in step and call
`token.ThrowIfCancellationRequested()` in loops of the async variant.

## Adding a property end to end

The model of an XML-attribute property is `SpecHierarchy.IsTableInternal`. Excerpts from
`ReqIFSharp/SpecElementWithAttributes/SpecHierarchy.cs` on `development`:

```csharp
// excerpt: ReqIFSharp/SpecElementWithAttributes/SpecHierarchy.cs (development)
[ReqIfProperty(aggregation: AggregationKind.None, lowerValue: 0, upperValue: 1, isOrdered: false, isReadOnly: false, isDerived: false, isDerivedUnion: false, isUnique: true, defaultValue: null)]
public bool IsTableInternal { get; set; }
```

```csharp
// excerpt: ReqIFSharp/SpecElementWithAttributes/SpecHierarchy.cs (development)
var isTableInternal = reader.GetAttribute("IS-TABLE-INTERNAL");
if (!string.IsNullOrWhiteSpace(isTableInternal))
{
    try
    {
        this.IsTableInternal = XmlConvert.ToBoolean(isTableInternal);
    }
    catch (Exception e)
    {
```

```csharp
// excerpt: ReqIFSharp/SpecElementWithAttributes/SpecHierarchy.cs (development)
internal override void WriteXmlAttributes(XmlWriter writer)
{
    base.WriteXmlAttributes(writer);

    if (this.IsTableInternal)
    {
        writer.WriteAttributeString("IS-TABLE-INTERNAL", "true");
    }
}
```

Checklist (DERIVED from how recent changes were made):

1. Look up the feature with `metamodel-lookup`: its multiplicity, whether it is an XML attribute,
   an owned child element (with or without a wrapper), or a `-REF` reference, and the XML name.
   `reqif-schema-lookup` gives the raw XSD type (`xsd:ID`, `xsd:dateTime`, `REQIF:LOCAL-REF`).
2. Add the property with XML docs and a `[ReqIfProperty(...)]` decorator. Lists are get-only
   and initialized (`public List<T> Items => this.items;`); single references are settable.
3. Read it in `ReadXml` **and** `ReadXmlAsync`: an attribute with `reader.GetAttribute`, a value
   converted with `XmlConvert` and a `SerializationException` with line and position on failure;
   a child element as a new `case` in the `LocalName` switch; a reference by looking the
   identifier up in the owner's `ReqIFContent` list, logging a `Trace` message when it is not
   found (see `SpecObject.ReadSpecType`).
4. Write it in `WriteXml` **and** `WriteXmlAsync` (attributes in `WriteXmlAttributes(Async)`),
   in schema order for elements; omit optional values that are not set, unless the
   distinction matters for round-tripping (see `IsEditableSpecified`, #86).
5. A new concrete subtype also needs its `ReqIfFactory` entries, a `[ReqIfClass]` decorator, the
   constructor triad, and, for attribute values, a branch in `SpecElementWithAttributes`.
6. If ReqIFSharp.Extensions should expose it, add a static method to the matching
   `*Extensions` class with `ArgumentNullException` checks and an `InvalidOperationException`
   when a required back-reference is missing.
7. Tests (below), a CHANGELOG line, and the wiki page if users see the change.

## Tests

NUnit 4 with `Assert.That`, fixtures named `*TestFixture`, test files from `TestData` (each file
needs a `<None Update="TestData\..."><CopyToOutputDirectory>Always</CopyToOutputDirectory></None>`
entry in the test `.csproj`). Helpers on `development`: `TestDocuments.Read(fileName)`,
`TestDocuments.ToStream(xml, kind)`, `TestDocuments.ComparableFiles`, `TestDocuments.Kinds`, and
`TestLogEventSink` to assert on Serilog output. Cover the sync and the async path and both
`.reqif` and `.reqifz`. A round-trip test in the house style (compiles and passes against
`development`):

```csharp
// new file: ReqIFSharp.Tests/SpecHierarchyRoundTripTestFixture.cs (development)
namespace ReqIFSharp.Tests
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using NUnit.Framework;

    using ReqIFSharp;

    /// <summary>
    /// Suite of tests for the round trip of <see cref="SpecHierarchy.IsTableInternal"/>
    /// </summary>
    [TestFixture]
    public class SpecHierarchyRoundTripTestFixture
    {
        [Test]
        public async Task Verify_that_IsTableInternal_is_written_and_read_again([ValueSource(typeof(TestDocuments), nameof(TestDocuments.Kinds))] SupportedFileExtensionKind kind)
        {
            var deserializer = new ReqIFDeserializer();

            using var source = TestDocuments.ToStream(TestDocuments.Read("reqifsharpgenerated.reqif"), SupportedFileExtensionKind.Reqif);
            var reqif = deserializer.Deserialize(source, SupportedFileExtensionKind.Reqif).Single();

            var leaf = AllNodes(reqif).First(x => x.Children.Count == 0);
            leaf.IsTableInternal = true;

            using var written = new MemoryStream();
            await new ReqIFSerializer().SerializeAsync(new[] { reqif }, written, kind, CancellationToken.None);

            written.Position = 0;
            var readAgain = (await deserializer.DeserializeAsync(written, kind, CancellationToken.None)).Single();

            var leafAgain = AllNodes(readAgain).Single(x => x.Identifier == leaf.Identifier);

            Assert.That(leafAgain.IsTableInternal, Is.True);
        }

        /// <summary>
        /// Returns every <see cref="SpecHierarchy"/> of the document, at any depth
        /// </summary>
        private static IEnumerable<SpecHierarchy> AllNodes(ReqIF reqif)
        {
            IEnumerable<SpecHierarchy> All(IEnumerable<SpecHierarchy> nodes) => nodes.SelectMany(x => new[] { x }.Concat(All(x.Children)));

            return All(reqif.CoreContent.Specifications.SelectMany(x => x.Children));
        }
    }
}
```

## CHANGELOG, CLA, branches, CI

- Every user-visible change adds a line under `## [Unreleased]` in `CHANGELOG.md` in the same
  pull request: Keep a Changelog groups (Added, Changed, Deprecated, Removed, Fixed, Security),
  the issue link, lines that only concern ReqIFSharp.Extensions start with
  "ReqIFSharp.Extensions:", breaking changes start with **Breaking:**.
- On release: rename `[Unreleased]` to `[X.Y.Z] - YYYY-MM-DD`, name the ReqIFSharp.Extensions
  version below it, add a fresh `[Unreleased]`, update the link list, copy the entries into
  `<PackageReleaseNotes>` of the affected `.csproj` files, bump `<Version>`. The two packages
  version independently; a ReqIFSharp release is normally followed by a ReqIFSharp.Extensions
  release that references it (9.2.2 was an exception).
- A signed CLA (`CLA/`, individual or corporate) is required before a code contribution is
  accepted.
- Branches: fork, branch off `development`, rebase on `upstream/development`, PR into
  `development` from a feature branch (never from `development` or `master`). `master` follows
  the latest release. The PR template asks for a descriptive title, no overlapping PRs, the
  style guide, and tests.
- CI (GitHub Actions): `CodeQuality.yml` (restore, build with `ContinuousIntegrationBuild=true`,
  test under `dotnet-coverage`, SonarCloud analysis), `codeql-analysis.yml`,
  `nuget-reference-check.yml`, and the manual `release.yml` (version in `CITATION.cff`, pack,
  push to nuget.org, tag, draft GitHub release).

## Known bugs worth a pull request (found while writing this skill)

- `SpecHierarchy.DeserializeSpecHierarchy(Async)` assigns each child's `IS-TABLE-INTERNAL` to the
  **parent** (`this.IsTableInternal = isTableInternal`), so a parent without the attribute reads
  as `true` when a child has `IS-TABLE-INTERNAL="true"`. Present in 10.1.2 and on `development`.
  The `SerializationException` message there also says "INTEGER" for a boolean.
- `AttributeDefinitionExtensions.QueryDefaultValueAsFormattedString` returns
  `EnumValue.ToString()` (the type name) for enumerations.
- `AttributeValueExtensions.QueryFormattedValue` formats enumerations with
  `Properties.OtherContent` instead of the literal's `LongName`.
- `SpecObjectExtensions.QuerySpecRelations` lacks the `ReqIFContent` null check its siblings have.
- `ExternalObject.QueryLocalData(string, Stream)` opens the archive with `FileMode.Open`
  (read/write access), so it fails on read-only files.
- `ExternalObject` detection in `AttributeValueXHTML` requires the literal prefix `xhtml:`.
- The "is not supported" warning fires for valid start and wrapper elements: reading the
  schema-valid file from `writing.md` logs 32 such warnings, on 10.1.2 and on `development`.

All of these were checked against the `development` source of October 2026.

Check the issue tracker before filing (`gh issue list --state all --search "<terms>"`).
