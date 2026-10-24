---
name: reqif-schema-lookup
description: Answer structural questions about the OMG ReqIF 1.2 XML schema itself (reqif.xsd and the XHTML driver.xsd) - a schema complex type's XML attributes and child elements, minOccurs/maxOccurs, LOCAL-REF vs GLOBAL-REF, the REQ-IF root element, the tool-extension wildcard, and which XHTML elements are allowed in ATTRIBUTE-VALUE-XHTML/THE-VALUE. Use when the user asks schema-level questions such as "what attributes does SPEC-OBJECT-TYPE have", "is DESC required", "can I put a <script> in XHTML content", "what is allowed in TOOL-EXTENSIONS", or is debugging an XSD validation error.
---

# ReqIF schema lookup

`<plugin-root>` is the plugin root from the reqifsharp-sage SessionStart context (or two
directories above this skill's base directory). The version `<v>` comes from
`<plugin-root>/knowledge/installed.json` (`default`). If it is missing, use `knowledge-setup`.

## Read order

1. `<plugin-root>/knowledge/<v>/schema/index.json` is an array of
   `{name, kind (complexType|simpleType), metamodelClass, globalElement, file}`.
2. `<plugin-root>/knowledge/<v>/schema/types/<NAME>.md` lists the attributes (type, use, default,
   fixed), the child elements (occurs, type, what a wrapper may contain, inner occurrence range),
   wildcards, and **Used by** (where else the type appears).
3. `<plugin-root>/knowledge/<v>/schema/xhtml-subset.md` lists the XHTML modules `driver.xsd`
   includes, its content groups, and the elements reachable from `XHTML-CONTENT`.
4. For anything the pages do not cover (facets, exact XSD text), read the raw schema at
   `<plugin-root>/sources/<v>/schema/reqif.xsd` or `driver.xsd`. The XHTML module schemas themselves
   (W3C XHTML Modularization 1.1) are not fetched. Say so if a question needs them.

## Things to get right

- Every concrete element carries `IDENTIFIER` (`xsd:ID`, required) and `LAST-CHANGE`
  (`xsd:dateTime`, required). `LONG-NAME`, `DESC` and `IS-EDITABLE` are optional.
- Because `IDENTIFIER` is an `xsd:ID` it must be an NCName: it cannot start with a digit or contain
  spaces. This is a frequent cause of validation errors in tool exports.
- A `-REF` element is `LOCAL-REF` (an `xsd:IDREF` that must resolve in the same document) or
  `GLOBAL-REF` (a plain string).
- `REQ-IF-TOOL-EXTENSION` holds `xsd:any namespace="##other" processContents="lax"`: tool data
  must be in a non-ReqIF namespace.
- XHTML content must be in the `http://www.w3.org/1999/xhtml` namespace, and only the subset from
  `driver.xsd` is valid.

## Answering

Tag schema facts as **MODEL** (read from the OMG XSD). Point to the metamodel view
(`metamodel-lookup`) when the user really asks about meaning rather than syntax. For a whole file
that fails validation, use `reqif-file-inspection`.
