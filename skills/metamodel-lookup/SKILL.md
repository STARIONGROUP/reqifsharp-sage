---
name: metamodel-lookup
description: Look up the structure of the OMG ReqIF 1.2 metamodel - a ReqIF class's attributes and associations, multiplicities, containment, generalizations, and exactly how each feature is written in a .reqif file (XML element names such as SPEC-OBJECT, wrapper elements, -REF references, required XML attributes). Use when the user asks "what is a SpecHierarchy", "what does SpecObject inherit", "is AttributeDefinition abstract", "how is a SpecRelation serialized", "what goes inside DEFAULT-VALUE", or names any ReqIF metaclass such as Identifiable, SpecType, DatatypeDefinitionEnumeration or AttributeValueXHTML.
---

# Metamodel lookup

Answers questions about the **ReqIF 1.2 abstract syntax** (the CMOF metamodel `reqif.cmof`) and
its **XML serialization** (`reqif.xsd`), joined on one page per class. Related skills:

- verbatim specification wording: `reqif-spec-citation`;
- raw schema types, e.g. `LOCAL-REF` or the XHTML subset: `reqif-schema-lookup`;
- the C# class that implements a metaclass: `reqifsharp-api-lookup`.

`<plugin-root>` is the plugin root from the reqifsharp-sage SessionStart context (or two
directories above this skill's base directory).

## Read order

1. `<plugin-root>/knowledge/installed.json` gives the `default` ReqIF version (normally `1.2`). If
   the file or the version is missing, use the `knowledge-setup` skill and stop.
2. `<plugin-root>/knowledge/<v>/metamodel/index.json` is an array of
   `{name, kind, package, qualifiedName, isAbstract, xmlElement, file}`. Match the name
   case-insensitively. Users often type the XML name (`SPEC-OBJECT`), so match `xmlElement` too.
3. `<plugin-root>/knowledge/<v>/metamodel/elements/<Name>.md` has these sections:
   - front matter;
   - `## Description`, the CMOF comment;
   - `## Generalizations` and `## Specializations`, plus the ancestor chain;
   - `## Owned features` and `## Inherited features` tables: type, multiplicity, kind
     (value / composite / reference), opposite end, and the **XML** column, e.g.
     `<TYPE> → <SPEC-OBJECT-TYPE-REF>` or `attribute LONG-NAME (optional)`;
   - `## XML serialization`: the XML attributes with their use, the child elements with occurrence
     ranges, which options are references by IDENTIFIER, and a skeleton;
   - `## CMOF tags`.

## ReqIF facts the pages encode

- A feature marked **not serialized** is the back-end of an association that the file does not
  store (e.g. `SpecHierarchy.parent`, `AttributeDefinition.specType`). It is reconstructed from
  containment when a file is read.
- `xsd:all` content allows any child order. Only `REQ-IF` itself uses `xsd:sequence`
  (`THE-HEADER`, `CORE-CONTENT`, `TOOL-EXTENSIONS`).
- `-REF` elements contain the target's `IDENTIFIER`. `LOCAL-REF` is an `xsd:IDREF` (the target must
  be in the same document); `GLOBAL-REF` is not checked by the schema.
- Abstract classes (`Identifiable`, `AccessControlledElement`, `SpecElementWithAttributes`,
  `SpecType`, `AttributeDefinition`, `AttributeValue`, `DatatypeDefinition`, `...Simple`) never
  appear as elements. Their features show up on every concrete subclass's element.

## Set and closure questions

Use `jq` over `<plugin-root>/knowledge/<v>/metamodel/metamodel.json`. Each class has
`allAncestors`, `allDescendants`, `ownedAttributes` and `inheritedAttributes`. Each feature has
`type`, `typeKind`, `lower`, `upper`, `isComposite`, `kind`, `opposite` and `xml{kind, name, contains[]}`.
Examples:

- `jq -r '.classes[] | select(.isAbstract|not) | select(.allAncestors|index("SpecElementWithAttributes")) | .name' metamodel.json`
- `jq -r '.classes[] | select(.name=="SpecRelation") | (.ownedAttributes+.inheritedAttributes)[] | "\(.name) \(.lower)..\(.upper) \(.xml.name // "-")"' metamodel.json`
- `jq -r '.classes[] | .ownedAttributes[] | select(.isComposite) | "\(.owner).\(.name) -> \(.type)"' metamodel.json` (the containment tree)

For many-class sweeps, delegate to the `metamodel-navigator` agent.

## Answering

- State the version ("In ReqIF 1.2, ...").
- Tag facts read from these files as **MODEL** (from the OMG CMOF/XSD). Skeletons and anything you
  infer are **DERIVED**. For **NORMATIVE** wording, cite the spec through `reqif-spec-citation`;
  class descriptions are in clause 10.8.x.
- When the user is writing code, mention the matching reqifsharp type (see `reqifsharp-api-lookup`).
