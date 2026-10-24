# reqifsharp pitfalls

Concrete gotchas of **ReqIFSharp 10.1.2 / ReqIFSharp.Extensions 5.2.2**, each found in the source
and, where marked *(run)*, reproduced against the packages. The snippets compile against those
packages; they assume `reqif` is a document read from a file (`writing.md` produces one).
"Fixed after 10.1.2" means fixed on the `development` branch and listed under `[Unreleased]` in
the CHANGELOG, not released.

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Schema;

using ReqIFSharp;
```

## 1. Version skew: docs describe an unreleased API

The ReqIFSharp wiki page "Writing ReqIF" uses `ReqIF.Create`, `content.CreateSpecObject`,
`CreateAttributeValue`, `AddChild`, `Identifiable.CreateIdentifier()` and `MarkAsChanged()`, and
the README on `development` mentions `AttributeValueXHTML.ExtractNormalizedTextFromValue`. None
of these, nor `IsEditableSpecified`, exists in 10.1.2 (they are on `development`, after 10.1.2). Check the version the project references
before suggesting them; `writing.md` shows the 10.1.2 way.

## 2. Constructors and ownership

- Public constructors never attach the new object to an owner or set back-references; you add
  it to the owner's list **and** set the back-reference yourself (table in `writing.md`).
  Forgetting the list means the element is not written; forgetting the back-reference breaks
  ReqIFSharp.Extensions (`InvalidOperationException`/`NullReferenceException`) and
  `ExternalObject.CreateUrl()`.
- The `internal` constructors used by the deserializer do attach: reading an unresolved
  relation `SOURCE`/`TARGET` therefore **adds a placeholder `SpecObject`** (no `Type`) to
  `CoreContent.SpecObjects`, and an unresolved relation-group specification adds a placeholder
  `Specification`. Code that counts or iterates spec objects sees them. *(run)*
- `ExternalObject(AttributeValueXHTML owner)` sets `Owner` but does not add the object to
  `owner.ExternalObjects`.
- Collections (`Values`, `SpecObjects`, `Children`, `SpecAttributes`, `SpecifiedValues`, ...)
  are get-only and never null: mutate them with `Add`/`AddRange`/`Clear`; you cannot assign a
  new list. *(run)* `ReqIF.ToolExtension` is the exception (settable).

## 3. Typed properties: the definition must match the value

- Every value has a typed `Definition` (`AttributeValueString.Definition` is an
  `AttributeDefinitionString`), so a mismatched kind does not compile. The base-class setters
  `AttributeValue.AttributeDefinition`, `AttributeDefinition.DatatypeDefinition` and
  `SpecElementWithAttributes.SpecType` check the **exact** runtime type
  (`GetType() != typeof(...)`) and throw `ArgumentException`
  ("specType must of type SpecObjectType") *(run)*; subclasses of the model types are rejected.
- Nothing checks that the definition is one of the element's `Type.SpecAttributes`, that an
  enumeration literal belongs to the definition's datatype, that a non-multi-valued
  enumeration has at most one literal, or that numbers and strings respect `Min`/`Max`/`MaxLength`.
  The schema cannot catch these either: a literal of another enumeration validates and reads
  back. *(run)* Check them yourself:

```csharp
IEnumerable<string> CheckValues(SpecElementWithAttributes element)
{
    var allowed = element.SpecType?.SpecAttributes ?? new List<AttributeDefinition>();

    foreach (var value in element.Values)
    {
        if (!allowed.Contains(value.AttributeDefinition))
        {
            yield return $"{element.Identifier}: {value.AttributeDefinition?.LongName} is not an attribute of {element.SpecType?.LongName}";
        }

        if (value is AttributeValueEnumeration enumeration)
        {
            var literals = enumeration.Definition.Type.SpecifiedValues;

            if (enumeration.Values.Any(literal => !literals.Contains(literal)))
            {
                yield return $"{element.Identifier}: literal outside {enumeration.Definition.Type.LongName}";
            }

            if (!enumeration.Definition.IsMultiValued && enumeration.Values.Count > 1)
            {
                yield return $"{element.Identifier}: several literals for single-valued {enumeration.Definition.LongName}";
            }
        }
    }
}

var valueProblems = reqif.CoreContent.SpecObjects.SelectMany(CheckValues).ToList();
```

- A default value needs `Definition` set to its own definition, or writing throws
  `NullReferenceException`.
- `ObjectValue` setters: `AttributeValueString` throws `NullReferenceException` for `null`
  *(run)*, `AttributeValueXHTML` throws `ArgumentNullException`, the enumeration setter needs an
  `IEnumerable<EnumValue>`, the others throw `InvalidOperationException` for a wrong type.

## 4. Identifiers

- `IDENTIFIER` is an `xsd:ID`: an NCName (starts with a letter or `_`, no spaces or colons),
  unique across the whole document. A GUID starting with a digit fails validation *(run)*; use
  `"_" + Guid.NewGuid()`. The serializer only rejects null or empty identifiers
  (`SerializationException`), except on the header: a null `ReqIFHeader.Identifier` is written
  as `IDENTIFIER=""` (invalid) and names the archive entry `0_.reqif`. *(run)*
- **Duplicate identifiers** make reading throw `InvalidOperationException` ("Sequence contains
  more than one matching element") as soon as a reference to one of them is resolved, with or
  without validation. *(run)* There is no lenient mode; repair the file first.
- References are resolved by identifier within one `ReqIF`; elements of another document in the
  same `.reqifz` cannot be referenced.

## 5. LAST-CHANGE and dates

- `LastChange` defaults to `DateTime.UtcNow` at construction (every `Identifiable`), is
  overwritten from the file when reading, and is **never updated** when you change an element.
  Set `element.LastChange = DateTime.UtcNow` after a change (`MarkAsChanged()` comes after
  10.1.2).
- An unparseable `LAST-CHANGE` (or `CREATION-TIME`, a date value, a boolean, `IS-EDITABLE`, ...)
  throws `SerializationException` with line and position; the read stops. *(run)*
- Dates are read with `XmlDateTimeSerializationMode.RoundtripKind`: `Z` gives `Kind == Utc`, an
  explicit offset is converted to the machine's **local** time (`Kind == Local`) *(run)*, no
  offset gives `Unspecified`. Writing uses round-trip format again, so the instant survives but
  the offset becomes the local one. Compare with `ToUniversalTime()`.
- `ReqIFHeader.CreationTime` defaults to `DateTime.MinValue`.

## 6. Null versus empty

| Property | `null` | empty |
|---|---|---|
| `LongName`, `Description` | attribute omitted | attribute omitted |
| `AttributeValueString.TheValue` | `THE-VALUE=""` | `THE-VALUE=""` |
| `AttributeValueXHTML.TheValue` | empty `THE-VALUE`, schema error *(run)* | same |
| header `Title`, `ReqIFToolId`, ... | empty element | empty element |
| `EnumValue.Properties` | no `PROPERTIES`, schema error *(run)* | n/a |
| `EmbeddedValue.OtherContent` | `OTHER-CONTENT=""` | same |
| an element's `Values` | n/a | no `VALUES`; every attribute takes its default |

When reading, absent XML attributes give `null` strings, `false`, `0` or `DateTime.MinValue`,
except `LastChange`, which keeps its construction time (now).

## 7. XHTML

- `TheValue` is written raw; it must be one well-formed `xhtml:div` or `xhtml:p` element. Plain
  text fails validation *(run)*. Malformed XML is written as is and breaks the whole file.
- The root element declares `xmlns:xhtml` only when `CoreContent.DataTypes` contains a
  `DatatypeDefinitionXHTML`. An XHTML value that uses the `xhtml` prefix without declaring it,
  in a document without that datatype, yields a malformed file (`XmlException`
  "'xhtml' is an undeclared prefix" on reading). *(run)* Declare the namespace on the value's
  root element.
- `ExternalObject`s are detected only by the literal text `xhtml:object`: XHTML in the default
  namespace (`<div xmlns="http://www.w3.org/1999/xhtml"><object .../></div>`) validates but
  gives no `ExternalObjects`. *(run)*
- `new AttributeValueXHTML(loggerFactory)` leaves `ExternalObjects` **null** *(run)*, so
  `QueryExternalObjects()` throws; use `new AttributeValueXHTML()` (fixed after 10.1.2, #98).
- Read values are trimmed; `TheOriginalValue` is never read (#95);
  `ExtractUnformattedTextFromValue()` puts a space between text nodes (`Some  bold  text`)
  *(run)*.

## 8. Reading quirks fixed only after 10.1.2

| Symptom in 10.1.2 | Cause | Issue |
|---|---|---|
| A file with `<reqif:REQ-IF xmlns:reqif="...">` reads as **zero documents**, no error *(run)* | the root is matched on its qualified name `REQ-IF` | #87 |
| A minified file (no whitespace between elements) loses every other header field (e.g. `REQ-IF-TOOL-ID`, `SOURCE-TOOL-ID`; *run*) and, per the CHANGELOG, every other enumeration literal, silently | `ReadElementContentAsString` followed by `Read()` skips a sibling | #94 |
| An element in a foreign namespace with a ReqIF local name is read as ReqIF | matching on local name only | #97 |
| `TheOriginalValue` always `null` after reading | not read | #95 |
| Nothing logged by attribute values, even with a logger factory | constructors drop the factory | #97 |

Not fixed on `development` either: when a `SpecHierarchy` reads its `CHILDREN`, it assigns
each child's `IS-TABLE-INTERNAL` to **itself**, so a parent without the attribute reads as
`IsTableInternal == true` when one of its children has `IS-TABLE-INTERNAL="true"` *(run)*.
Writing the document back then adds the attribute to the parent.

For the first two rows, a workaround is to normalize the XML first (DERIVED): load it with
`XDocument`, rewrite the root without a prefix or save it indented, then deserialize the result
from a `MemoryStream`.

## 9. Unresolved references

Summary (details in `reading.md`): unresolved `TYPE`s and hierarchy objects become `null` and
are logged at **Trace** only, while the `Warning` "... is not supported" fires for many valid
elements (false positives in 10.1.2) *(run)*, so the log level that looks alarming is the
uninformative one; relation ends become placeholder spec objects; an unresolved
attribute value `DEFINITION` throws `InvalidOperationException`. A document read with
placeholders cannot be written back: *(run)*

```csharp
var placeholders = reqif.CoreContent.SpecObjects.Where(o => o.Type == null).ToList();

if (placeholders.Count > 0)
{
    // SerializationException: "The Type property of SpecObject <id>: may not be null"
    Console.WriteLine($"{placeholders.Count} unresolved spec object(s): {string.Join(", ", placeholders.Select(o => o.Identifier))}");
}
```

Remove the placeholders and the relations that use them, or give them a type, before writing.

## 10. Writing quirks

- **Attachments are not written.** `Serialize` to `.reqifz` writes only `.reqif` entries; add the
  files with `ZipArchive` in `Update` mode (`writing.md`, step 9). A document read from an
  archive and written to a new one loses its images.
- **Infinite reals** are written as `Infinity`/`-Infinity`, not the `xsd:double` forms
  `INF`/`-INF` *(run)*. .NET's validator and reqifsharp accept it, other tools do not (fixed
  after 10.1.2, #85). `NaN` is written as `NaN`, which is valid.
- `IsEditable = false` is not written; an explicit `IS-EDITABLE="false"` read from a file is
  dropped on write (#86).
- An `AttributeDefinition` or `SpecHierarchy` with an `AlternativeId` **and** `IsEditable = true`
  (or `IsTableInternal = true`) throws `InvalidOperationException` ("Token StartAttribute in
  state Element Content would result in an invalid XML document") *(run)*, because the attribute
  is written after the `ALTERNATIVE-ID` element (#86). Other elements with an `AlternativeId`
  are fine.
- A `RelationGroup` with `Values` produces a schema-invalid file (no `VALUES` on
  `RELATION-GROUP` in the XSD).
- `Serialize(path)` creates or truncates the file before writing; an exception or a cancelled
  token leaves an **empty or partial file** *(run)*. Write to a temporary file and move it.
- Output is UTF-8 with a BOM *(run)*, indented, with no option to change either.

## 11. Streams and files

- Deserializer streams must be seekable (`Length`, `Seek`); the `.reqifz` path disposes your
  stream *(run)*; `ExternalObject.QueryLocalData(Stream, Stream)` disposes the archive stream.
- `QueryLocalData(string path, Stream)` opens the file for read/write and fails on read-only
  files with `UnauthorizedAccessException` *(run)*.
- Extension detection is case-sensitive (`.REQIF` throws `ArgumentException`) *(run)*.
- An archive entry is found only when its name ends in `.reqif`; a `.xml` inside a `.reqifz` is
  ignored.

## 12. Validation

- The handler receives warnings as well as errors; count only `XmlSeverityType.Error`. Content
  in `TOOL-EXTENSIONS` and other foreign-namespace elements produce warnings *(run)*.
- Without a handler, `validate: true` throws `XmlSchemaValidationException` on the first error.
- A handler with `validate: false` throws `ArgumentException`.

```csharp
bool IsSchemaValid(string path, out IReadOnlyList<string> errors)
{
    var found = new List<string>();
    new ReqIFDeserializer().Deserialize(path, validate: true, validationEventHandler: (sender, e) =>
    {
        if (e.Severity == XmlSeverityType.Error)
        {
            found.Add($"{e.Exception?.LineNumber}:{e.Exception?.LinePosition} {e.Message}");
        }
    });

    errors = found;
    return found.Count == 0;
}

Console.WriteLine(IsSchemaValid("requirements.reqif", out var schemaErrors) ? "valid" : string.Join(Environment.NewLine, schemaErrors));
```

## 13. Async and cancellation

- The token is checked between elements (`ThrowIfCancellationRequested`), not passed into
  `XmlReader`, so cancellation is cooperative and coarse; it throws `OperationCanceledException`.
  *(run)*
- `DeserializeAsync(Stream, ...)` is not an `async` method: its argument errors (null or empty
  stream, handler without `validate`) are thrown **synchronously** from the call, not from the
  returned task *(run)*; so is the null check of `ReqIFLoaderService.LoadAsync`. The path
  overloads throw them from the task.
- The path-based `DeserializeAsync` logs every exception except `OperationCanceledException` at
  `Error` before rethrowing.
- The sync methods have no cancellation; run large files with the async overloads.

## 14. Thread safety

- `ReqIFDeserializer` and `ReqIFSerializer` hold no per-call state; one instance can be shared
  across threads (DERIVED from the source).
- The model is plain mutable `List<T>`s and auto-properties, not thread-safe. Concurrent reads of
  a graph nobody mutates are fine; synchronize any mutation (DERIVED).
- `ReqIFLoaderService`: see `extensions.md` (shared source stream).

## 15. netstandard2.0 implications

- Both packages target `netstandard2.0`: .NET Framework 4.6.2+ and .NET (Core) 2.0+. They have no
  nullable annotations, so the compiler does not warn about the nulls above.
- Reals are written with `double.ToString(CultureInfo.InvariantCulture)`. On .NET Core 3.0+ that
  is the shortest round-trippable form (`0.30000000000000004`); on **.NET Framework** it is 15
  significant digits, so some reals do not round-trip exactly (DERIVED from the runtime's
  documented formatting).
- The minimum `Microsoft.Extensions.Logging.Abstractions` is 6.0.0 (since 10.1.0); your
  application's newer version wins.

## 16. Breaking changes across major versions (from the CHANGELOG)

| Version | Change | What to do |
|---|---|---|
| 9.0.0 (Extensions 4.0.0) | async methods that return a `Task` were renamed to end in `Async`, in both packages | rename calls from 8.x code |
| 9.0.0 | `ReqIFLoaderService` is `IDisposable` and takes an optional `ILoggerFactory` | dispose it |
| 10.0.0 (Extensions 5.0.0) | every `int` property became `long` (`DatatypeDefinitionInteger.Min`/`Max`, `DatatypeDefinitionString.MaxLength`, `DatatypeDefinitionReal.Accuracy`, `EmbeddedValue.Key`, `AttributeValueInteger.TheValue`); integer overflow while reading sets the default value | change variable types; casts from `long` to `int` may truncate |
| 10.0.0 | `SpecHierarchy.IsTableInternal` is read | none |
| 10.1.0 | root namespaces and attributes are retained on a round trip (#44); `ReqIfClass`/`ReqIfProperty` metadata attributes added (#27) | none |

ReqIFSharp.Extensions versions pair with ReqIFSharp: 4.x with 9.x, 5.x with 10.x (5.2.2 with
10.1.2).
