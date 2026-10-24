# Reading ReqIF with reqifsharp

Applies to **ReqIFSharp 10.1.2** (and ReqIFSharp.Extensions 5.2.2 where noted). Every snippet
compiles against those packages and was run on the files that `writing.md` produces
(`requirements.reqif`, `requirements.reqifz` with the attachment `files/tank.png`).

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Schema;

using Microsoft.Extensions.Logging;

using ReqIFSharp;
```

## Entry points (API)

`ReqIFDeserializer` implements `IReqIFDeSerializer`; its only constructor is
`ReqIFDeserializer(ILoggerFactory loggerFactory = null)`.

```csharp
// signatures (ReqIFSharp 10.1.2, IReqIFDeSerializer)
IEnumerable<ReqIF> Deserialize(string fileUri, bool validate = false, ValidationEventHandler validationEventHandler = null);
IEnumerable<ReqIF> Deserialize(Stream stream, SupportedFileExtensionKind fileExtensionKind, bool validate = false, ValidationEventHandler validationEventHandler = null);
Task<IEnumerable<ReqIF>> DeserializeAsync(string fileUri, CancellationToken token, bool validate = false, ValidationEventHandler validationEventHandler = null);
Task<IEnumerable<ReqIF>> DeserializeAsync(Stream stream, SupportedFileExtensionKind fileExtensionKind, CancellationToken token, bool validate = false, ValidationEventHandler validationEventHandler = null);
```

All four return a fully materialized `List<ReqIF>` typed as `IEnumerable<ReqIF>`: one element for
a `.reqif` file, one per `.reqif` entry for an archive. The deserializer keeps no state between
calls, so one instance can be shared (DERIVED from the source: its only fields are the logger
and logger factory).

## From a file

```csharp
var deserializer = new ReqIFDeserializer();

var reqif = deserializer.Deserialize("requirements.reqif").Single();
var fromArchive = (await deserializer.DeserializeAsync("requirements.reqifz", cancellationToken)).First();
```

The extension picks the format through `SupportedFileExtensionKindExtensions.ConvertPathToSupportedFileExtensionKind`:
`.reqif` is XML, `.reqifz` and `.zip` are zip archives. The comparison is **case-sensitive**:
`Export.REQIF` throws `ArgumentException("only .reqif, .reqifz and .zip are supported file extensions.")`.
Open such a file as a stream and pass the kind instead. A `null` path throws
`ArgumentNullException`, an empty one `ArgumentOutOfRangeException`.

## From a stream

```csharp
await using (var file = File.OpenRead("requirements.reqif"))
{
    var fromStream = await deserializer.DeserializeAsync(file, SupportedFileExtensionKind.Reqif, cancellationToken);
}

async Task<IEnumerable<ReqIF>> ReadUploadAsync(Stream uploadStream, SupportedFileExtensionKind kind, CancellationToken token)
{
    using var buffer = new MemoryStream();
    await uploadStream.CopyToAsync(buffer, token);
    return await new ReqIFDeserializer().DeserializeAsync(buffer, kind, token);
}
```

- `SupportedFileExtensionKind` has two members: `Reqif` and `Reqifz` (use `Reqifz` for `.zip`).
- The stream must be **seekable**: the deserializer reads `stream.Length` (empty throws
  `ArgumentException`) and seeks to position 0 itself. Network and ASP.NET request streams are
  not; buffer them as in `ReadUploadAsync`.
- With `Reqifz`, the stream is **disposed** when the call returns (the internal `ZipArchive` is
  opened without `leaveOpen`). With `Reqif`, it stays open.

## Archives with several documents and attachments

```csharp
foreach (var document in deserializer.Deserialize("requirements.reqifz"))
{
    Console.WriteLine($"{document.TheHeader.Title}: {document.CoreContent.SpecObjects.Count} spec objects");
}
```

Every archive entry whose name ends in `.reqif` (case-insensitive, in any folder) is read, in
archive order; an archive without one throws `FileNotFoundException("No reqif file could be found in the archive.")`.
Each entry becomes its own `ReqIF` with its own `CoreContent`; references are resolved only
within one document.

Attachments are the other entries of the archive. reqifsharp finds them through the
`xhtml:object` elements of XHTML values: each becomes an `ExternalObject` in
`AttributeValueXHTML.ExternalObjects` with `Uri` (the `data` attribute, URL-unescaped),
`MimeType` (`type`), `Width` and `Height` (`int?`) and `Owner`.

```csharp
var archiveDocument = deserializer.Deserialize("requirements.reqifz").First();
Directory.CreateDirectory("attachments");

foreach (var xhtml in archiveDocument.CoreContent.SpecObjects.SelectMany(o => o.Values).OfType<AttributeValueXHTML>())
{
    foreach (var externalObject in xhtml.ExternalObjects.Where(e => e.IsDataLocal()))
    {
        using var archiveStream = File.OpenRead("requirements.reqifz");   // QueryLocalData disposes it
        using var output = File.Create(Path.Combine("attachments", Path.GetFileName(externalObject.Uri)));
        externalObject.QueryLocalData(archiveStream, output);
    }
}
```

- `IsDataLocal()` is `false` when `Uri` starts with `http` (case-insensitive); download those
  yourself. It throws `NullReferenceException` when `Uri` is null.
- `QueryLocalData(Stream reqifz, Stream target)` copies the entry named exactly `Uri` into
  `target` and **disposes `reqifz`**. A missing entry writes nothing and does not throw.
- `QueryLocalData(string reqifPath, Stream target)` opens the file with `FileMode.Open`, which
  requests read/write access: it throws `UnauthorizedAccessException` on a read-only file. The
  stream overload with `File.OpenRead`, as above, avoids that.
- Only objects written with the `xhtml` prefix are found: detection requires the literal text
  `xhtml:object` in the value. An `<object>` in the default namespace, or under another prefix,
  gives no `ExternalObject`.
- ReqIFSharp.Extensions offers `QueryExternalObjects()` and the caching `ReqIFLoaderService`
  (data URIs); see `extensions.md`.

## Schema validation

Validation happens while reading, against the ReqIF 1.x XSD and its XHTML modules, which are
embedded in the assembly (no network access).

```csharp
var findings = new List<string>();

var validated = deserializer.Deserialize("requirements.reqif", validate: true, validationEventHandler: (sender, e) =>
{
    findings.Add($"{e.Severity} at {e.Exception?.LineNumber}:{e.Exception?.LinePosition}: {e.Message}");
}).Single();

var hasErrors = findings.Any(f => f.StartsWith(nameof(XmlSeverityType.Error), StringComparison.Ordinal));

try
{
    deserializer.Deserialize("requirements.reqif", validate: true);
}
catch (XmlSchemaValidationException e)
{
    Console.WriteLine($"Not valid ReqIF: {e.Message} (line {e.LineNumber})");
}
```

- With a handler, reading continues after a schema problem: you get the document and all
  findings. Without one, the first **error** throws `XmlSchemaValidationException`.
- The handler also receives **warnings** (`ReportValidationWarnings` is set), for example
  "Could not find schema information for the element ..." for tool-extension content in a
  foreign namespace. Filter on `e.Severity == XmlSeverityType.Error` before calling a file
  invalid.
- A handler with `validate: false` throws `ArgumentException` (`validationEventHandler must be null when validate is false`).
- Validation does not protect against reqifsharp's own failures: duplicate identifiers throw
  `InvalidOperationException` ("Sequence contains more than one matching element") during
  reference resolution, and an unresolvable value definition throws too (see `pitfalls.md`).
- For a full conformance report (schema plus metamodel and Implementation Guide rules) use the
  `reqif-file-inspection` skill.

## Logging

```csharp
using var loggerFactory = LoggerFactory.Create(builder => builder
    .AddConsole()                      // package Microsoft.Extensions.Logging.Console
    .SetMinimumLevel(LogLevel.Trace));

var loggingDeserializer = new ReqIFDeserializer(loggerFactory);
var logged = loggingDeserializer.Deserialize("requirements.reqif");
```

What is logged (API, from the source): `Warning` for every element that is skipped
("The {LocalName} element at line:position {LineNumber}:{LinePosition} is not supported").
In 10.1.2 this warning also fires for many **valid** elements that are read correctly: reading
the schema-valid file of `writing.md` logs about 30 of them (`THE-HEADER`, `CORE-CONTENT`,
`REQ-IF-CONTENT`, each `SPEC-OBJECT`, `VALUES`, `TYPE`, `DEFAULT-VALUE`, `SPEC-HIERARCHY`,
`SOURCE`, `TARGET`, ...) because nested readers see their own start and wrapper elements. Do
not treat these warnings as data loss; compare the element name with what you expected.

`Trace` covers timings, every element read, and **unresolved references** (a type, spec object or
datatype that could not be found and was set to null, or a placeholder that was created). So
turn on `Trace` when a document reads "successfully" but has null `Type`s. `Error` is logged
for an exception in the path-based `DeserializeAsync`. In 10.1.2 attribute values never log, because
the `AttributeValue` constructors drop the logger factory (fixed after 10.1.2, #97).

## Navigating the object graph (API)

| From | Property | Type |
|---|---|---|
| `ReqIF` | `TheHeader`, `CoreContent`, `ToolExtension`, `Lang` | `ReqIFHeader`, `ReqIFContent`, `List<ReqIFToolExtension>`, `string` (`xml:lang`) |
| `ReqIFHeader` | `Identifier`, `Title`, `Comment`, `CreationTime`, `RepositoryId`, `ReqIFToolId`, `ReqIFVersion`, `SourceToolId`, `DocumentRoot` | strings, `DateTime`, `ReqIF` |
| `ReqIFContent` | `DataTypes`, `SpecTypes`, `SpecObjects`, `SpecRelations`, `Specifications`, `SpecRelationGroups`, `DocumentRoot` | get-only `List<...>`s, `ReqIF` |
| any `Identifiable` | `Identifier`, `LongName`, `Description` (`DESC`), `LastChange`, `AlternativeId` | strings, `DateTime`, `AlternativeId` |
| `SpecObject`, `Specification`, `SpecRelation`, `RelationGroup` | `Values`, `Type` (typed), `SpecType` (base), `ReqIFContent` | `List<AttributeValue>`, ... |
| `SpecType` | `SpecAttributes`, `ReqIFContent` | `List<AttributeDefinition>` |
| `AttributeDefinition*` | `Type` (typed datatype), `DatatypeDefinition` (base), `DefaultValue`, `SpecType`, `IsEditable`; `IsMultiValued` on enumerations | |
| `DatatypeDefinitionEnumeration` | `SpecifiedValues` | `List<EnumValue>` |
| `EnumValue` | `Properties.Key` (`long`), `Properties.OtherContent`, `DataTpeDefEnum` | |

Collections are never null (they are initialized and get-only); single references can be null.

```csharp
var content = reqif.CoreContent;

Console.WriteLine($"{reqif.TheHeader.Title} by {reqif.TheHeader.SourceToolId}, created {reqif.TheHeader.CreationTime:u}");

foreach (var specObjectType in content.SpecTypes.OfType<SpecObjectType>())
{
    Console.WriteLine($"{specObjectType.LongName}: {string.Join(", ", specObjectType.SpecAttributes.Select(a => a.LongName))}");
}
```

## Reading attribute values

Each value has `AttributeDefinition` (base type, typed per subclass as `Definition`) and a typed
value property. Switch on the subclass:

```csharp
string Describe(AttributeValue value) => value switch
{
    AttributeValueString s => s.TheValue,
    AttributeValueInteger i => i.TheValue.ToString(System.Globalization.CultureInfo.InvariantCulture),
    AttributeValueReal r => r.TheValue.ToString(System.Globalization.CultureInfo.InvariantCulture),
    AttributeValueBoolean b => b.TheValue ? "yes" : "no",
    AttributeValueDate d => d.TheValue.ToString("yyyy-MM-dd"),
    AttributeValueEnumeration e => string.Join(", ", e.Values.Select(literal => literal.LongName)),
    AttributeValueXHTML x => x.ExtractUnformattedTextFromValue(),
    _ => value.ObjectValue?.ToString()
};

foreach (var specObject in content.SpecObjects)
{
    foreach (var value in specObject.Values)
    {
        Console.WriteLine($"{specObject.LongName}.{value.AttributeDefinition.LongName} = {Describe(value)}");
    }
}
```

Find one value by attribute name, falling back to the definition's default when the object has
no value for it (DERIVED; ReqIF says a missing value means the default applies):

```csharp
EnumValue StatusOf(SpecObject specObject)
{
    var definition = specObject.Type.SpecAttributes.OfType<AttributeDefinitionEnumeration>().Single(a => a.LongName == "Status");
    var value = specObject.Values.OfType<AttributeValueEnumeration>().SingleOrDefault(v => v.Definition == definition) ?? definition.DefaultValue;
    return value?.Values.FirstOrDefault();
}

var brewStatus = StatusOf(content.SpecObjects.Single(o => o.LongName == "REQ-1"));
Console.WriteLine($"{brewStatus.LongName} (key {brewStatus.Properties.Key})");
```

Enumeration values: `Values` holds the selected `EnumValue` literals (several only when the
definition `IsMultiValued`). A literal has `LongName`, `Properties.Key` and
`Properties.OtherContent` (often empty). An `ENUM-VALUE-REF` that matches no literal in **any**
enumeration datatype of the document is silently dropped; a literal of another datatype is
accepted.

Integers are `long` (since 10.0.0; a value that overflows `long` is read as 0).

### XHTML: markup or text

`AttributeValueXHTML.TheValue` is the inner XML of `THE-VALUE`, trimmed. The reader adds the
namespace declarations in scope, so the root element carries `xmlns:xhtml` and the string is a
self-contained XML fragment:

```text
<xhtml:div xmlns:xhtml="http://www.w3.org/1999/xhtml">The machine shall brew a cup of coffee in <xhtml:b>under 30 seconds</xhtml:b>.</xhtml:div>
```

```csharp
var text = content.SpecObjects.Single(o => o.LongName == "REQ-1").Values.OfType<AttributeValueXHTML>().Single();

var markup = text.TheValue;                                 // render as HTML, or parse with XDocument
var plain = text.ExtractUnformattedTextFromValue();         // text nodes joined with " "
var normalized = Regex.Replace(plain, @"\s+", " ").Trim();  // collapse the doubled spaces
```

`ExtractUnformattedTextFromValue()` joins the text nodes with a space, so
`Some <b>bold</b> text` becomes `Some  bold  text` and `<p>a</p><p>b</p>` becomes `a b`.
`ExtractNormalizedTextFromValue(bool keepLineBreaks = false)`, which does this properly, exists
only after 10.1.2 (#92). `IsSimplified` is read; `TheOriginalValue` is **not** read in 10.1.2
(always `null` after reading, #95).

## Walking a specification

`Specification.Children` holds the top-level `SpecHierarchy` nodes. A node has `Object` (the
`SpecObject`), `Children`, `Container` (parent node, `null` at top level), `Root` (the
specification), `IsTableInternal` and `EditableAtts`.

```csharp
void Print(IEnumerable<SpecHierarchy> nodes, int depth)
{
    foreach (var node in nodes)
    {
        var title = node.Object?.LongName ?? "<unresolved spec object>";
        Console.WriteLine($"{new string(' ', depth * 2)}{title}");
        Print(node.Children, depth + 1);
    }
}

foreach (var specification in content.Specifications)
{
    Console.WriteLine($"{specification.LongName} ({specification.Type?.LongName})");
    Print(specification.Children, 1);
}
```

`node.Object` is `null` when its `SPEC-OBJECT-REF` did not resolve. For a flat list in document
order use `specification.QueryAllContainedSpecObjects()` from ReqIFSharp.Extensions.

## Trace links

```csharp
foreach (var relation in content.SpecRelations)
{
    Console.WriteLine($"{relation.Source.LongName} --{relation.Type?.LongName}--> {relation.Target.LongName}");
}

foreach (var group in content.SpecRelationGroups)
{
    Console.WriteLine($"{group.LongName}: {group.SpecRelations.Count} relations from {group.SourceSpecification.LongName} to {group.TargetSpecification.LongName}");
}
```

`Source` and `Target` are never null after reading: an unresolved reference is replaced by a
placeholder `SpecObject` (see below). ReqIFSharp.Extensions adds `specObject.QuerySpecRelations()`.

## Unresolved references while reading (API)

reqifsharp resolves a reference by looking up identifiers among the elements read **so far**.
The schema fixes the order of `REQ-IF-CONTENT` (`DATATYPES`, `SPEC-TYPES`, `SPEC-OBJECTS`,
`SPEC-RELATIONS`, `SPECIFICATIONS`, `SPEC-RELATION-GROUPS`), so this works for valid files; a
file that puts, say, `SPEC-OBJECTS` before `SPEC-TYPES` reads with null types.

| Reference | When not found |
|---|---|
| `TYPE` of a spec object, specification, relation or relation group | set to `null`, Trace log (none for a specification) |
| attribute definition's `TYPE` (datatype) | set to `null`, Trace log |
| `SPEC-OBJECT-REF` of a `SpecHierarchy` | `Object` is `null`, Trace log |
| `SOURCE`/`TARGET` of a `SpecRelation` | a new placeholder `SpecObject` with that `Identifier`, `Description` "This spec-object was not found in the source file.", no `Type`, **added to `CoreContent.SpecObjects`** |
| `SOURCE-SPECIFICATION`/`TARGET-SPECIFICATION` of a relation group | placeholder `Specification`, likewise |
| `DEFINITION` of an attribute value | **throws `InvalidOperationException`** ("The attribute-definition ... could not be found for the value."), reading stops |
| `ENUM-VALUE-REF` | literal dropped silently |
| `SPEC-RELATION-REF` of a relation group | relation left out, Trace log |
| duplicate `IDENTIFIER` among candidates | **throws `InvalidOperationException`** |

Writing such a document again fails on the null types (see `pitfalls.md`).
