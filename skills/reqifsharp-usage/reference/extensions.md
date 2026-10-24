# ReqIFSharp.Extensions

Applies to **ReqIFSharp.Extensions 5.2.2** (built on ReqIFSharp 10.1.2; the `development` branch
changed only source headers since). Separate NuGet package, `netstandard2.0`. Every snippet
compiles and was run on the files that `writing.md` produces.

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using ReqIFSharp;
using ReqIFSharp.Extensions;                  // StringExtensions (Base64)
using ReqIFSharp.Extensions.ReqIFExtensions;  // the query extension methods
using ReqIFSharp.Extensions.Services;         // IReqIFLoaderService, ReqIFLoaderService
```

The methods navigate through the back-references (`ReqIFContent`, `SpecElAt`, `DocumentRoot`).
The deserializer sets all of them; on a document you build yourself, set them as `writing.md`
shows, or the methods throw `InvalidOperationException` ("The owning ReqIFContent of the ... is
not set") or `NullReferenceException`. All query methods are eager (they return a built
`List`/`HashSet`) except the `QueryReferencing*` methods on spec types, which return a lazy
`Where` over the live list.

## Navigating specifications (API)

| Method | Returns | Notes |
|---|---|---|
| `Specification.QueryAllContainedSpecHierarchies()` | `IEnumerable<SpecHierarchy>` | every node at any depth, depth-first pre-order (document order) |
| `SpecHierarchy.QueryAllContainedSpecHierarchies()` | `IEnumerable<SpecHierarchy>` | the nodes below this node, not the node itself |
| `Specification.QueryAllContainedSpecObjects()` | `IEnumerable<SpecObject>` | `node.Object` of every node in document order; contains duplicates when an object is referenced twice and `null` for unresolved nodes |
| `Specification.QueryAttributeDefinitions()` | `IEnumerable<AttributeDefinition>` | the definitions that have a value on at least one contained spec object (a `HashSet`, order undefined); the columns of a table view |
| `SpecObject.QueryContainerSpecifications()` | `IEnumerable<Specification>` | the specifications whose hierarchy contains the object (compared by `Identifier`); throws when `ReqIFContent` is null |

```csharp
var reqif = new ReqIFDeserializer().Deserialize("requirements.reqif").Single();
var specification = reqif.CoreContent.Specifications.Single();

var columns = specification.QueryAttributeDefinitions().OrderBy(c => c.LongName).ToList();
Console.WriteLine(string.Join(" | ", columns.Select(c => c.LongName)));

foreach (var row in specification.QueryAllContainedSpecObjects().Where(o => o != null))
{
    var cells = columns.Select(column => row.Values.FirstOrDefault(v => v.AttributeDefinition == column)?.QueryFormattedValue() ?? "");
    Console.WriteLine(string.Join(" | ", cells));
}
```

## Trace links (API)

| Method | Returns | Notes |
|---|---|---|
| `SpecObject.QuerySpecRelations()` | `IEnumerable<SpecRelation>` | relations in which the object is `Source` or `Target` (reference equality); `NullReferenceException` when `ReqIFContent` is null (no friendly check here) |
| `SpecRelationType.QueryReferencingSpecRelations()` | `IEnumerable<SpecRelation>` | relations of that type |

```csharp
var brew = reqif.CoreContent.SpecObjects.Single(o => o.LongName == "REQ-1");

foreach (var relation in brew.QuerySpecRelations())
{
    var outgoing = relation.Source == brew;
    var other = outgoing ? relation.Target : relation.Source;
    Console.WriteLine($"{(outgoing ? "outgoing" : "incoming")} {relation.Type.LongName}: {other.LongName}");
}
```

## Where is a type used? (API)

| Method | Returns |
|---|---|
| `DatatypeDefinition.QueryReferencingAttributeDefinitions()` | `IEnumerable<AttributeDefinition>`, every attribute definition of every spec type whose datatype is this one |
| `SpecObjectType.QueryReferencingSpecObjects()` | `IEnumerable<SpecObject>` |
| `SpecificationType.QueryReferencingSpecifications()` | `IEnumerable<Specification>` |
| `RelationGroupType.QueryReferencingRelationGroups()` | `IEnumerable<RelationGroup>` |

`QueryReferencingAttributeDefinitions` throws `NullReferenceException`, not
`ArgumentNullException`, for a null argument.

```csharp
var unusedDatatypes = reqif.CoreContent.DataTypes.Where(d => !d.QueryReferencingAttributeDefinitions().Any()).ToList();
var requirementType = reqif.CoreContent.SpecTypes.OfType<SpecObjectType>().Single();
Console.WriteLine($"{requirementType.QueryReferencingSpecObjects().Count()} objects of type {requirementType.LongName}");
```

## Display strings (API)

| Method | Returns |
|---|---|
| `AttributeValue.QueryFormattedValue()` | invariant-culture string: `bool` as `True`/`False`; dates as `yyyy-MM-dd, HH:mm:ss`; integers and reals via `ToString(InvariantCulture)`; strings as is; **XHTML as raw markup**; **enumerations as the `Properties.OtherContent` of the selected literals joined by `;`** (usually empty, and `NullReferenceException` when a literal has no `Properties`) |
| `AttributeDefinition.QueryDefaultValueAsFormattedString()` | the default value formatted as above (dates as `MMMM dd, yyyy`), or `"NOT SET"`. For an enumeration it returns `EnumValue.ToString()`, which is the type name `ReqIFSharp.EnumValue` (bug), or `null` when the default has no literal |
| `AttributeDefinition.QueryDatatypeName()`, `DatatypeDefinition.QueryDatatypeName()` | `Boolean`, `Date`, `Enumeration`, `Integer`, `Real`, `String` or `XHTML` |
| `Identifiable.QueryTypeDisplayName()` | e.g. `Spec Object`, `Enumeration Attribute Definition`, `Relation Group Type`; throws `InvalidOperationException` for an unknown subtype |

For display, prefer your own formatting for enumerations and XHTML (DERIVED):

```csharp
string Display(AttributeValue value) => value switch
{
    AttributeValueEnumeration e => string.Join(", ", e.Values.Select(literal => literal.LongName)),
    AttributeValueXHTML x => x.ExtractUnformattedTextFromValue(),
    _ => value.QueryFormattedValue()
};

foreach (var attribute in requirementType.SpecAttributes)
{
    Console.WriteLine($"{attribute.LongName}: {attribute.QueryDatatypeName()} ({attribute.QueryTypeDisplayName()})");
}

Console.WriteLine(string.Join("; ", brew.Values.Select(Display)));
```

## Attachments (API)

| Method | Returns |
|---|---|
| `ReqIFContent.QueryExternalObjects()` | `IEnumerable<ExternalObject>` of all XHTML values of relation groups, specifications, spec objects and spec relations (in that order). Default values of attribute definitions are not included |
| `SpecElementWithAttributes.QueryExternalObjects()` | the `ExternalObject`s of one element's XHTML values |
| `SpecElementWithAttributes.QueryBase64PayloadsAsync(IReqIFLoaderService reqIfLoaderService, CancellationToken token = default)` | `Task<IEnumerable<Tuple<ExternalObject, string>>>`: each object with its `data:` URI from the loader service |
| `ExternalObject.CreateUrl()` | `"/reqif/{TheHeader.Identifier}/externalobject/{Uri.Base64Encode()}"`; needs `Owner.SpecElAt.ReqIFContent.DocumentRoot.TheHeader`, so it throws `NullReferenceException` for a value you created without `SpecElAt`. Standard Base64 can contain `/` and `+`; encode the segment for routing |

## ReqIFLoaderService (API)

`ReqIFLoaderService : IReqIFLoaderService, IDisposable` loads one ReqIF source, keeps a private
copy of the bytes, and serves attachment data from it with a per-object cache. It is the
service behind the ReqIF web viewer.

```csharp
// signatures (ReqIFSharp.Extensions 5.2.2)
public ReqIFLoaderService(IReqIFDeSerializer reqIfDeSerializer, ILoggerFactory loggerFactory = null);
IEnumerable<ReqIF> ReqIFData { get; }
Task LoadAsync(Stream reqifStream, SupportedFileExtensionKind fileExtensionKind, CancellationToken token);
Task<string> QueryDataAsync(ExternalObject externalObject, CancellationToken token);
Task<Stream> GetSourceStreamAsync(CancellationToken token);
void Reset();
event EventHandler<IEnumerable<ReqIF>> ReqIfChanged;
```

```csharp
using var loader = new ReqIFLoaderService(new ReqIFDeserializer());
loader.ReqIfChanged += (sender, documents) => Console.WriteLine($"loaded {documents.Count()} document(s)");

await using (var source = File.OpenRead("requirements.reqifz"))
{
    await loader.LoadAsync(source, SupportedFileExtensionKind.Reqifz, cancellationToken);
}

var loaded = loader.ReqIFData.First();

foreach (var externalObject in loaded.CoreContent.QueryExternalObjects())
{
    var dataUri = await loader.QueryDataAsync(externalObject, cancellationToken);   // "data:image/png;base64,iVBOR..."
    Console.WriteLine($"{externalObject.Uri} ({externalObject.MimeType}): {dataUri.Length} characters");
}

var tank = loaded.CoreContent.SpecObjects.Single(o => o.LongName == "REQ-3");

foreach (var (externalObject, dataUri) in await tank.QueryBase64PayloadsAsync(loader, cancellationToken))
{
    Console.WriteLine($"<img src=\"{dataUri}\" width=\"{externalObject.Width}\" />");
}

await using var copy = await loader.GetSourceStreamAsync(cancellationToken);   // a new MemoryStream copy each call
```

Behaviour (API, from the source):

- `LoadAsync` seeks the input stream to 0 and copies it twice (a kept copy and a deserialization
  copy), so the input may be closed afterwards but must be seekable. It never validates. It
  clears the cache, replaces `ReqIFData`, then raises `ReqIfChanged`.
- `QueryDataAsync` returns `data:{MimeType};base64,{bytes}` and caches it per `ExternalObject`
  instance. It reads the kept copy as a zip archive, so after loading a plain `.reqif` it throws
  `InvalidDataException`; an `http` URI throws `InvalidOperationException`; a missing archive
  entry returns `data:{MimeType};base64,` with no payload.
- `GetSourceStreamAsync` and `QueryDataAsync` before `LoadAsync`, after `Reset()` or after
  `Dispose()` throw `NullReferenceException` (no `ObjectDisposedException`).
- `Reset()` disposes the copy, sets `ReqIFData` to `null`, clears the cache and raises
  `ReqIfChanged` with an empty sequence.
- Concurrency (DERIVED from the source): the cache is a `ConcurrentDictionary`, but every cache
  miss seeks and copies the one shared source stream, and `LoadAsync` replaces it. Do not call
  `LoadAsync`/`Reset` while queries run, and serialize concurrent `QueryDataAsync` calls (for
  example with a `SemaphoreSlim`) if they can miss the cache at the same time.

Dependency injection, as the web viewer does it (DERIVED): one deserializer for the
application, one loader per user session.

```csharp
// services.AddSingleton<IReqIFDeSerializer, ReqIFDeserializer>();
// services.AddScoped<IReqIFLoaderService, ReqIFLoaderService>();
```

`ReqIFDeserializer` has a single constructor with an optional `ILoggerFactory`, so containers
resolve it with the application's logger factory.

## Base64 helpers (API)

`ReqIFSharp.Extensions.StringExtensions`: `string Base64Encode(this string text)` (UTF-8, then
standard Base64) and `string Base64Decode(this string text)`. Both throw on `null`
(`ArgumentNullException`); `Base64Decode` throws `FormatException` for invalid input.

```csharp
var encoded = "files/tank.png".Base64Encode();
var decoded = encoded.Base64Decode();
```
