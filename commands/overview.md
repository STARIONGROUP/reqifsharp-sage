---
description: What the reqifsharp-sage plugin can do and how to set it up.
disable-model-invocation: true
---

# reqifsharp-sage

Sage is the AI agent of the reqifsharp ecosystem. It answers questions about the **OMG
Requirements Interchange Format (ReqIF) 1.2** and the **reqifsharp** C# library, from a knowledge
base built on your machine:

- the ReqIF metamodel (CMOF) joined with the XML schema, so each class shows how it is written in
  a `.reqif` file;
- the normative specification text, clause by clause;
- the prostep ivip ReqIF Implementation Guide, if you import your own copy (it is not public);
- the reqifsharp API (ReqIFSharp and ReqIFSharp.Extensions), mapped to ReqIF classes and XML
  elements;
- an `inspect` verb that checks `.reqif`/`.reqifz` files against the schema, the metamodel rules and
  the guide's recommendations.

## First-time setup

Nothing OMG- or prostep-derived ships with the plugin (see `NOTICE`). Ask Claude to "set up the
ReqIF knowledge base", or run the CLI (Claude is told its path at session start):

```bash
reqifsharp-sage fetch --version 1.2          # spec PDF, reqif.cmof, reqif.xsd, driver.xsd from omg.org
                                             #   (or: --from <folder> to use local copies)
reqifsharp-sage import-guide <guide.pdf>     # optional: your copy of the prostep Implementation Guide
reqifsharp-sage generate --version 1.2       # builds everything; takes a few seconds
reqifsharp-sage check                        # what is ready
```

## What you can ask

| Skill | Example |
|---|---|
| metamodel-lookup | "What are the features of SpecHierarchy and how is it serialized?" |
| reqif-schema-lookup | "Which XHTML elements may appear in THE-VALUE?" |
| reqif-spec-citation | "Quote the spec on AttributeDefinitionEnumeration.multiValued." |
| implementation-guide-citation | "What does the implementation guide recommend for ReqIF.ForeignID?" |
| reqifsharp-api-lookup | "What overloads does ReqIFDeserializer.DeserializeAsync have?" |
| reqifsharp-usage | "Show me how to write a .reqifz with two specifications and an attachment." |
| reqif-file-inspection | "Check export.reqifz for problems." |
| knowledge-setup | "Is the ReqIF knowledge base up to date?" |

Answers are tagged by provenance:

- **NORMATIVE**: verbatim spec text;
- **GUIDE**: prostep Implementation Guide, informative;
- **MODEL**: OMG CMOF/XSD;
- **API**: the reqifsharp reference;
- **DERIVED**: inferred.
