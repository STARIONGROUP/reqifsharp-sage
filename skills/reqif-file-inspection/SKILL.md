---
name: reqif-file-inspection
description: Check a user-supplied .reqif or .reqifz FILE for problems - XML schema errors, duplicate or missing IDENTIFIERs, dangling -REF references, attribute values whose definition or type does not match their SpecType, enumeration and range violations, broken SpecHierarchy/SpecRelation/RelationGroup links, XHTML outside the allowed subset, missing attachments, and prostep Implementation Guide recommendations (ReqIF.* system attribute names, encoding, file extension). Use when the user shares a ReqIF file or archive and asks to validate it, check it, find out why a tool rejects it, or why reqifsharp throws while reading it.
---

# ReqIF file inspection

`<cli>` is the reqifsharp-sage CLI and `<plugin-root>` the plugin root, both named in the
SessionStart context. If that context is missing, see `knowledge-setup`, "Finding the CLI".

## Run

```bash
<cli> inspect "<file.reqif|file.reqifz>" --json --repository-root "<plugin-root>"
```

- The exit code is 1 when there are errors, 0 otherwise.
- The guide rules (G-xxx) run when an Implementation Guide is imported.
  - `--no-guide-rules` turns them off.
  - `--guide-rules` forces them on without an import. The rules are the project's own paraphrased
    checks, so they work without the guide text.
- `<cli> inspect --rules --json` lists every rule: `{id, severity, category, reference, description, isGuideRule}`.

The output is `{file, guideRulesApplied, errorCount, warningCount, infoCount, documents[], findings[]}`:

- `documents[]`: `{document, identifier, title, reqIFVersion, sourceToolId, reqIFToolId, specObjectCount, specificationCount, specRelationCount, datatypeCount, specTypeCount}`.
  A `.reqifz` has one entry per `.reqif` inside it.
- `findings[]`: `{severity, ruleId, category, identifier, message, line, reference, document}`.
  They are ordered by document, then line, then rule.

## Rule families

| Ids | What |
|---|---|
| `LOAD` | Not readable, not well-formed, not a REQ-IF document, or reqifsharp throws while reading it |
| `XSD` | Schema validation against the reqif.xsd/driver.xsd that reqifsharp embeds (ReqIF 1.2 §11) |
| `READER` | Warnings reqifsharp logs while reading |
| `R-001`–`R-034` | ReqIF metamodel rules: identifiers, references and their kinds, value/definition/datatype consistency, ranges, enumerations, defaults, LONG-NAME, relation groups, hierarchy, header, XHTML and attachments |
| `G-000`–`G-017` | prostep ivip Implementation Guide 1.7 recommendations (informative) |

The semantic rules work on the XML with line numbers rather than on the reqifsharp object graph.
That way they still explain files reqifsharp cannot load (duplicate identifiers, unresolved
definitions and wrong-kind references all make it throw).

## Answering

1. Lead with the verdict: "N errors, M warnings in `<file>`" and the documents found.
2. Group the findings by category. For each group, give the location (document, line,
   IDENTIFIER), what is wrong, why it matters and how to fix it.
3. Back the rule with the knowledge base when useful:
   - `metamodel-lookup` for the class or feature involved;
   - `reqif-schema-lookup` for XSD errors;
   - `reqif-spec-citation` for the clause in `reference`;
   - `implementation-guide-citation` for G-xxx.
4. Severity: errors make a file invalid or unreadable by reqifsharp; warnings are likely
   interoperability problems; info is advisory. G-xxx findings are recommendations, not
   conformance failures; say so.
5. If the user's code produced the file, point to the likely cause in their reqifsharp usage
   (`reqifsharp-usage`, see `reference/pitfalls.md`).

For reports with many findings, pass the JSON to the `reqif-inspector` agent instead of explaining
every finding inline. Do not paste a confidential file's content back to the user beyond what is
needed to explain a finding.
