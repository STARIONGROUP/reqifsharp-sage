# Writing ReqIF with reqifsharp

Applies to **ReqIFSharp 10.1.2**. Everything below compiles and was run against that package:
the example writes a `.reqif` and a `.reqifz`, reads both back and validates them against the
ReqIF schema with zero errors. The `development` branch adds builder methods
(`ReqIF.Create`, `content.CreateSpecObject(...)`, ...) that are **not** in 10.1.2; see
[Next release](#next-release-builder-methods-not-in-1012) at the end.

## The model in one paragraph (API)

In 10.1.2 every **public** constructor creates a detached object: it does not add itself to any
list and does not set back-references. The only side effects are that `Identifiable` sets
`LastChange = DateTime.UtcNow` and `new AttributeValueXHTML()` creates an empty
`ExternalObjects` list. The constructors that attach an element to its owner (for example
`internal SpecObject(ReqIFContent, ILoggerFactory)`, which adds itself to `SpecObjects`) are
`internal` and used only by the deserializer. So when you build a document you do two things for
every element: **add it to its owner's list** (that is what gets written) and **set its
back-references** (the serializer ignores them, but ReqIFSharp.Extensions and your own code use
them).

| Element | Add it to | Set on it |
|---|---|---|
| `ReqIFHeader` | `reqif.TheHeader` | `Identifier` (required), `DocumentRoot` |
| `ReqIFContent` | `reqif.CoreContent` | `DocumentRoot` |
| `DatatypeDefinition*` | `content.DataTypes` | `Identifier`, `ReqIFContent` |
| `EnumValue` | `enumeration.SpecifiedValues` | `Identifier`, `DataTpeDefEnum`, `Properties` (required by the schema) |
| `EmbeddedValue` | `enumValue.Properties` | `Key`, `OtherContent`, `EnumValue` |
| `SpecObjectType`, `SpecificationType`, `SpecRelationType`, `RelationGroupType` | `content.SpecTypes` | `Identifier`, `ReqIFContent` |
| `AttributeDefinition*` | `specType.SpecAttributes` | `Identifier`, `LongName`, `Type` (the datatype), `SpecType` |
| `SpecObject` | `content.SpecObjects` | `Identifier`, `Type`, `ReqIFContent` |
| `AttributeValue*` | `element.Values` | `Definition`, the value, `SpecElAt` |
| `Specification` | `content.Specifications` | `Identifier`, `Type`, `ReqIFContent` |
| `SpecHierarchy` | `specification.Children` or `parentNode.Children` | `Identifier`, `Object`, `Root`, `Container` (null at top level), `ReqIfContent` |
| `SpecRelation` | `content.SpecRelations` | `Identifier`, `Type`, `Source`, `Target`, `ReqIFContent` |
| `RelationGroup` | `content.SpecRelationGroups` | `Identifier`, `LongName`, `Type`, `SourceSpecification`, `TargetSpecification`, `ReqIFContent` and `CoreContent` |
| `ReqIFToolExtension` | `reqif.ToolExtension` | `InnerXml` |

Spelling traps, all API: the back-reference on `SpecHierarchy` is `ReqIfContent` (lower-case
`f`); the one on other elements is `ReqIFContent`; `RelationGroup` has both `ReqIFContent` and
`CoreContent` (the deserializer resolves specifications through `CoreContent`); the owner of an
`EnumValue` is `DataTpeDefEnum` (sic).

What the serializer checks (API): a `SerializationException` when an `Identifiable` has no
`Identifier`, or when a required reference is null (`SpecObject.Type`, `Specification.Type`,
`SpecRelation.Type`/`Source`/`Target`, `RelationGroup.Type`/`SourceSpecification`/`TargetSpecification`,
`SpecHierarchy.Object`, an attribute definition's `Type`, a value's `Definition`). It does
**not** check that identifiers are valid `xsd:ID`s or unique, that referenced elements are in
`CoreContent`, that a value's definition belongs to the element's type, that enumeration
literals belong to the definition's datatype, or that numbers are within `Min`/`Max`. Validate
the written file (step 9) to catch those.

## The complete example

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Schema;

using ReqIFSharp;
```

### 1. Identifiers, root, header and content

An `IDENTIFIER` must be a valid `xsd:ID` (an NCName: starts with a letter or `_`, no spaces or
colons) and unique in the document. A bare GUID can start with a digit, so prefix it (DERIVED;
the next release adds `Identifiable.CreateIdentifier()`, which does the same).

```csharp
string NewId() => $"_{Guid.NewGuid()}";

var content = new ReqIFContent();

var reqif = new ReqIF
{
    Lang = "en",
    TheHeader = new ReqIFHeader
    {
        Identifier = NewId(),
        Title = "Coffee machine requirements",
        Comment = "Written with ReqIFSharp 10.1.2",
        CreationTime = DateTime.UtcNow,
        ReqIFVersion = "1.0",
        ReqIFToolId = "my-exporter",
        SourceToolId = "my-exporter"
    },
    CoreContent = content
};

reqif.TheHeader.DocumentRoot = reqif;
content.DocumentRoot = reqif;
```

`new ReqIF()` leaves `TheHeader` and `CoreContent` null; writing it then throws a
`NullReferenceException`. A default `ReqIFHeader` writes `IDENTIFIER=""` (schema-invalid) and
`CREATION-TIME` `0001-01-01T00:00:00`, so set at least `Identifier` and `CreationTime`.
`ReqIFVersion` is `"1.0"` for every ReqIF 1.x document (1.0.1, 1.1 and 1.2 share the schema and
namespace).

### 2. Datatypes

```csharp
var stringType = new DatatypeDefinitionString { Identifier = NewId(), LongName = "String255", MaxLength = 255, ReqIFContent = content };
var xhtmlType = new DatatypeDefinitionXHTML { Identifier = NewId(), LongName = "FormattedText", ReqIFContent = content };
var integerType = new DatatypeDefinitionInteger { Identifier = NewId(), LongName = "Priority", Min = 1, Max = 5, ReqIFContent = content };
var realType = new DatatypeDefinitionReal { Identifier = NewId(), LongName = "Seconds", Accuracy = 2, Min = 0, Max = 3600, ReqIFContent = content };
var booleanType = new DatatypeDefinitionBoolean { Identifier = NewId(), LongName = "Flag", ReqIFContent = content };
var dateType = new DatatypeDefinitionDate { Identifier = NewId(), LongName = "Date", ReqIFContent = content };
var statusType = new DatatypeDefinitionEnumeration { Identifier = NewId(), LongName = "Status", ReqIFContent = content };

content.DataTypes.AddRange(new DatatypeDefinition[] { stringType, xhtmlType, integerType, realType, booleanType, dateType, statusType });

EnumValue AddLiteral(DatatypeDefinitionEnumeration enumeration, string longName, long key)
{
    var literal = new EnumValue { Identifier = NewId(), LongName = longName, DataTpeDefEnum = enumeration };
    literal.Properties = new EmbeddedValue { Key = key, OtherContent = string.Empty, EnumValue = literal };
    enumeration.SpecifiedValues.Add(literal);
    return literal;
}

var draft = AddLiteral(statusType, "Draft", 0);
var approved = AddLiteral(statusType, "Approved", 1);
```

Integer, real and string datatypes always write `MIN`/`MAX`, `ACCURACY` and `MAX-LENGTH`
(defaults 0), so set them. An `EnumValue` without `Properties` writes no `PROPERTIES` element,
which the schema requires. `OtherContent` is a required XML attribute; `null` is written as
`OTHER-CONTENT=""`.

The root element gets `xmlns:xhtml="http://www.w3.org/1999/xhtml"` only when
`content.DataTypes` contains a `DatatypeDefinitionXHTML` (and the document did not already carry
that declaration when it was read).

### 3. Spec types and attribute definitions

```csharp
var requirementType = new SpecObjectType { Identifier = NewId(), LongName = "Requirement", ReqIFContent = content };
var documentType = new SpecificationType { Identifier = NewId(), LongName = "Document", ReqIFContent = content };
var refinesType = new SpecRelationType { Identifier = NewId(), LongName = "refines", ReqIFContent = content };
var traceSetType = new RelationGroupType { Identifier = NewId(), LongName = "Trace set", ReqIFContent = content };

content.SpecTypes.AddRange(new SpecType[] { requirementType, documentType, refinesType, traceSetType });

TDefinition AddAttribute<TDefinition>(SpecType owner, TDefinition definition, string longName)
    where TDefinition : AttributeDefinition
{
    definition.Identifier = NewId();
    definition.LongName = longName;
    definition.SpecType = owner;
    owner.SpecAttributes.Add(definition);
    return definition;
}

var foreignIdAttribute = AddAttribute(requirementType, new AttributeDefinitionString { Type = stringType }, "ReqIF.ForeignID");
var textAttribute = AddAttribute(requirementType, new AttributeDefinitionXHTML { Type = xhtmlType }, "ReqIF.Text");
var statusAttribute = AddAttribute(requirementType, new AttributeDefinitionEnumeration { Type = statusType, IsMultiValued = false }, "Status");
var priorityAttribute = AddAttribute(requirementType, new AttributeDefinitionInteger { Type = integerType }, "Priority");
var durationAttribute = AddAttribute(requirementType, new AttributeDefinitionReal { Type = realType }, "Brew time");
var safetyAttribute = AddAttribute(requirementType, new AttributeDefinitionBoolean { Type = booleanType }, "Safety relevant");
var dueAttribute = AddAttribute(requirementType, new AttributeDefinitionDate { Type = dateType }, "Due");

statusAttribute.DefaultValue = new AttributeValueEnumeration { Definition = statusAttribute };
statusAttribute.DefaultValue.Values.Add(draft);
```

Each typed attribute definition has a typed `Type` (its datatype) and a typed `DefaultValue`.
The base-class `DatatypeDefinition` property also works, but its setter throws an
`ArgumentException` when the datatype kind does not match. A default value must have its
`Definition` set to the owning definition: the writer reads `DefaultValue.Definition.Identifier`
and throws a `NullReferenceException` otherwise. The attribute names `ReqIF.ForeignID` and
`ReqIF.Text` are the conventional names many tools use; ReqIF itself fixes no names.

### 4. Spec objects and attribute values

```csharp
SpecObject AddRequirement(string foreignId, string xhtmlBody, EnumValue status)
{
    var specObject = new SpecObject { Identifier = NewId(), LongName = foreignId, Type = requirementType, ReqIFContent = content };

    specObject.Values.Add(new AttributeValueString { Definition = foreignIdAttribute, TheValue = foreignId, SpecElAt = specObject });

    specObject.Values.Add(new AttributeValueXHTML
    {
        Definition = textAttribute,
        TheValue = $"<xhtml:div xmlns:xhtml=\"http://www.w3.org/1999/xhtml\">{xhtmlBody}</xhtml:div>",
        SpecElAt = specObject
    });

    var statusValue = new AttributeValueEnumeration { Definition = statusAttribute, SpecElAt = specObject };
    statusValue.Values.Add(status);
    specObject.Values.Add(statusValue);

    content.SpecObjects.Add(specObject);
    return specObject;
}

var brew = AddRequirement("REQ-1", "The machine shall brew a cup of coffee in <xhtml:b>under 30 seconds</xhtml:b>.", approved);
var heat = AddRequirement("REQ-2", "The water shall be heated to 92 °C before brewing starts.", draft);
var tank = AddRequirement("REQ-3",
    "<xhtml:p>The machine shall warn the user when the water tank is empty.</xhtml:p>" +
    "<xhtml:object data=\"files/tank.png\" type=\"image/png\" width=\"64\" height=\"64\">water tank icon</xhtml:object>",
    draft);

brew.Values.Add(new AttributeValueInteger { Definition = priorityAttribute, TheValue = 1, SpecElAt = brew });
brew.Values.Add(new AttributeValueReal { Definition = durationAttribute, TheValue = 29.5, SpecElAt = brew });
brew.Values.Add(new AttributeValueBoolean { Definition = safetyAttribute, TheValue = false, SpecElAt = brew });
brew.Values.Add(new AttributeValueDate { Definition = dueAttribute, TheValue = new DateTime(2027, 1, 31, 0, 0, 0, DateTimeKind.Utc), SpecElAt = brew });
```

Value types (API): `AttributeValueString.TheValue` `string`, `AttributeValueInteger.TheValue`
`long` (since 10.0.0), `AttributeValueReal.TheValue` `double`, `AttributeValueBoolean.TheValue`
`bool`, `AttributeValueDate.TheValue` `DateTime`, `AttributeValueXHTML.TheValue` `string`,
`AttributeValueEnumeration.Values` `List<EnumValue>` (get-only; add to it). `ObjectValue` is the
untyped accessor on every value.

Add only the values you want to fill in; a missing value means "use the definition's default".

How values are written (API): dates with `DateTime.ToString("o")` (a `DateTimeKind.Unspecified`
date gets no offset, a UTC one gets `Z`), reals with `double.ToString(CultureInfo.InvariantCulture)`,
`LAST-CHANGE` and `CREATION-TIME` with `XmlConvert` in round-trip mode.

### 5. XHTML values

`TheValue` is written **raw** (`XmlWriter.WriteRaw`) between `<THE-VALUE>` tags. It must be:

- well-formed XML with exactly one root element, `xhtml:div` or `xhtml:p` (the schema's
  `XHTML-CONTENT`); plain text, an empty string or `null` fails validation;
- self-contained: declare `xmlns:xhtml="http://www.w3.org/1999/xhtml"` on the root element as
  above (DERIVED advice). The reader returns values in exactly that form, and a value that only
  relies on the root's `xmlns:xhtml` produces a malformed file when the document has no
  `DatatypeDefinitionXHTML`;
- limited to the XHTML modules the schema allows (`reqif-schema-lookup` lists them).

`xhtml:object` elements reference attachments (step 8). `TheOriginalValue` with
`IsSimplified = true` writes `THE-ORIGINAL-VALUE`; note that 10.1.2 does not read it back.

### 6. Specification and hierarchy

```csharp
var specification = new Specification { Identifier = NewId(), LongName = "Coffee machine", Type = documentType, ReqIFContent = content };
content.Specifications.Add(specification);

SpecHierarchy AddNode(SpecHierarchy parent, SpecObject specObject)
{
    var node = new SpecHierarchy { Identifier = NewId(), Object = specObject, Root = specification, Container = parent, ReqIfContent = content };
    (parent == null ? specification.Children : parent.Children).Add(node);
    return node;
}

var brewNode = AddNode(null, brew);
AddNode(brewNode, heat);
AddNode(null, tank);
```

A spec object can be referenced from several nodes and several specifications. `IsTableInternal`
and `EditableAtts` are optional. `IsEditable` is written only when `true` in 10.1.2.

### 7. Relations and relation groups

```csharp
var heatRefinesBrew = new SpecRelation { Identifier = NewId(), Type = refinesType, Source = heat, Target = brew, ReqIFContent = content };
content.SpecRelations.Add(heatRefinesBrew);

var traceSet = new RelationGroup
{
    Identifier = NewId(),
    LongName = "Refinements",
    Type = traceSetType,
    SourceSpecification = specification,
    TargetSpecification = specification,
    ReqIFContent = content,
    CoreContent = content
};
traceSet.SpecRelations.Add(heatRefinesBrew);
content.SpecRelationGroups.Add(traceSet);
```

Relations carry attribute values like spec objects (give the `SpecRelationType` attribute
definitions). Do not give a relation group values: the metamodel allows them and reqifsharp
writes them, but the XML schema has no `VALUES` on `RELATION-GROUP`, so the file becomes invalid.

### 8. Tool extensions

```csharp
reqif.ToolExtension.Add(new ReqIFToolExtension
{
    InnerXml = "<my:settings xmlns:my=\"urn:example:my-exporter\"><my:view>flat</my:view></my:settings>"
});
```

`InnerXml` is written raw inside `<REQ-IF-TOOL-EXTENSION>` and read back as the inner XML of
that element, so it round-trips. Use your own namespace: the schema allows only elements from
other namespaces there (`processContents="lax"`, so the validator reports warnings, not errors,
for elements it has no schema for).

### 9. Write, add attachments, validate

```csharp
var serializer = new ReqIFSerializer();

serializer.Serialize(new[] { reqif }, "requirements.reqif");      // one ReqIF XML document
serializer.Serialize(new[] { reqif }, "requirements.reqifz");     // zip archive, one entry per ReqIF

await using (var stream = File.Create("requirements-async.reqifz"))
{
    await serializer.SerializeAsync(new[] { reqif }, stream, SupportedFileExtensionKind.Reqifz, cancellationToken);
}

// The serializer writes only the .reqif entries. Add the files that xhtml:object elements reference
// yourself, at the path given in their data attribute (tank.png is a file on disk).
using (var archive = ZipFile.Open("requirements.reqifz", ZipArchiveMode.Update))
{
    archive.CreateEntryFromFile("tank.png", "files/tank.png");
}

var errors = new List<string>();

new ReqIFDeserializer().Deserialize("requirements.reqifz", validate: true, validationEventHandler: (sender, e) =>
{
    if (e.Severity == XmlSeverityType.Error)
    {
        errors.Add($"line {e.Exception?.LineNumber}: {e.Message}");
    }
});

Console.WriteLine(errors.Count == 0 ? "valid ReqIF" : string.Join(Environment.NewLine, errors));
```

Serializer facts (API):

- The path overloads pick the format from the extension: `.reqif`, `.reqifz` or `.zip`
  (case-sensitive); anything else throws an `ArgumentException`. The file is created or
  overwritten (`FileMode.Create`).
- `.reqif` takes exactly one `ReqIF`, otherwise `ArgumentException`; `.reqifz` takes at least
  one. Archive entries are named `{index}_{TheHeader.Identifier}.reqif`.
- The stream overloads leave your stream open. Output is UTF-8 with BOM, indented, with an XML
  declaration; the root is `REQ-IF` in the default namespace
  `http://www.omg.org/spec/ReqIF/20110401/reqif.xsd`.
- `ReqIFSerializer` has no logger and no options (no indentation or encoding control).
- The async overloads check the token between elements; a cancelled token throws
  `OperationCanceledException`. The path overloads create or truncate the file first, so a
  failure leaves an empty or partial file.

## Modifying an existing document (round-tripping)

The deserializer sets every back-reference, so a document you read can be changed with the same
property assignments as above and written again.

```csharp
var document = new ReqIFDeserializer().Deserialize("requirements.reqif").Single();

var tankRequirement = document.CoreContent.SpecObjects.Single(o => o.LongName == "REQ-3");
var tankStatus = tankRequirement.Values.OfType<AttributeValueEnumeration>().Single(v => v.Definition.LongName == "Status");

tankStatus.Values.Clear();
tankStatus.Values.Add(tankStatus.Definition.Type.SpecifiedValues.Single(literal => literal.LongName == "Approved"));
tankRequirement.LastChange = DateTime.UtcNow;   // not updated automatically

new ReqIFSerializer().Serialize(new[] { document }, "requirements-v2.reqif");
```

What survives a read/write cycle in 10.1.2 (API, from the source and its CHANGELOG):

- kept: all modelled content, the namespace declarations and other attributes of the root
  element (since 10.1.0), `xml:lang`, tool extensions;
- changed: whitespace around XHTML values is trimmed; formatting and element order are the
  writer's (indentation, attribute order, `IS-EDITABLE="false"` dropped);
- lost: `THE-ORIGINAL-VALUE` of XHTML values, XML comments and processing instructions, and any
  element reqifsharp does not model (skipped with a warning);
- blocked: a document with unresolved `SOURCE`/`TARGET` references reads with placeholder spec
  objects that have no `Type`, and writing it throws (see `pitfalls.md`);
- attachments in a `.reqifz` are not copied; add them to the new archive as in step 9.

## Next release: builder methods (not in 10.1.2)

The `development` branch (unreleased, after 10.1.2) adds methods that create an element, add it
to its owner, set its back-references, give it an identifier and set `LastChange`:
`ReqIF.Create(title, loggerFactory)`, `ReqIFContent.CreateDatatypeDefinitionString(longName, maxLength)`
and the other `CreateDatatypeDefinition*`, `CreateSpecObjectType`, `CreateSpecificationType`,
`CreateSpecRelationType`, `CreateRelationGroupType`, `CreateSpecObject(type, longName)`,
`CreateSpecification`, `CreateSpecRelation(type, source, target)`, `CreateRelationGroup`,
`DatatypeDefinitionEnumeration.CreateEnumValue(longName, key, otherContent)`,
`SpecType.CreateAttributeDefinition(datatype, longName)`, `Specification.AddChild` and
`SpecHierarchy.AddChild`, `SpecElementWithAttributes.CreateAttributeValue(definition, value)`
(validates the constraints the serializer above does not), `Identifiable.CreateIdentifier()`
and `Identifiable.MarkAsChanged()`. Do not use them in code that references ReqIFSharp 10.1.2;
it will not compile. The ReqIFSharp wiki page "Writing ReqIF" already documents them.
