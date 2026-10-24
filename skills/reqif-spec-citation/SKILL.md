---
name: reqif-spec-citation
description: Quote or cite the normative text of the OMG Requirements Interchange Format (ReqIF) 1.2 specification (formal/16-07-01) with an exact clause number. Use when the user asks "what does the ReqIF spec say about X", wants the official definition of a ReqIF concept (SpecHierarchy, AttributeDefinitionEnumeration.multiValued, isEditable, tool extensions, the exchange scenarios, conformance), asks which clause covers something, or needs a citable reference. Not for the prostep Implementation Guide (see implementation-guide-citation).
---

# ReqIF specification citation

`<plugin-root>` is the plugin root from the reqifsharp-sage SessionStart context (or two
directories above this skill's base directory). The version `<v>` comes from
`<plugin-root>/knowledge/installed.json` (`default`).

## Read order

1. Check `versions[].specGenerated` in `installed.json`. If it is false, or
   `<plugin-root>/knowledge/<v>/spec/index.json` does not exist, **do not quote from memory**:
   - give the clause number if you know it, tagged **DERIVED**;
   - say verbatim text is unavailable;
   - point to `knowledge-setup` (fetch without `--no-specs`, then generate).
2. `<plugin-root>/knowledge/<v>/spec/index.json` is an array of
   `{clause, title, document, pages, normative, file, license, redistributable}`. Search titles
   first. For concepts, `Grep` the `clauses/` folder.
3. Read `<plugin-root>/knowledge/<v>/spec/<file>`. Its front matter has the clause, title and pages.

## Where things are in ReqIF 1.2

- Clause 2: Conformance.
- Clause 4: Terms and Definitions.
- Clause 6.1: how to read the specification.
- Clause 7: Concept Overview and Use Cases (7.4 exchange scenarios, 7.5 detailed use cases).
- Clause 8: Abstract Architecture.
- Clause 9: Exchange Document Structure (9.2 class descriptions of `ReqIF`, `ReqIFHeader`,
  `ReqIFContent`, `ReqIFToolExtension`).
- Clause 10: Exchange Document Content:
  - 10.2 identification of elements;
  - 10.4 hierarchy and relations;
  - 10.5–10.6 data types;
  - 10.7 access restrictions;
  - 10.8.x one subclause per class, with Attributes, Associations, Constraints and Semantics.
- Clause 11: Production Rules of ReqIF XML Schema (11.4 is the EBNF).

## Quoting rules

- Quote verbatim and keep it short: only the sentences that answer the question. Give the reference
  as **"ReqIF 1.2 (formal/16-07-01) §<clause> <title>, p. <pages>"** and tag it **NORMATIVE**.
- Clauses whose front matter says `normative: false`, and blocks marked
  `<!-- informative:note|example -->`, are informative. Say so.
- The text is OMG copyright (`redistributable: false`). Quote it for the user's reading; do not
  paste whole clauses into files, commits or generated code.
- The extraction is from a PDF. Figures are not reproduced, and a table can occasionally be
  mangled. If a passage looks garbled, say so and give the page number.

For sweeps across many clauses ("every clause mentioning isEditable"), delegate to the
`spec-citation` agent.
