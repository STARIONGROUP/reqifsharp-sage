---
name: implementation-guide-citation
description: Cite the prostep ivip / VDA "ReqIF Implementation Guide" (v1.7) that the user imported themselves - its recommendations on file extensions (.reqif/.reqifz), encoding, the ReqIF.* system attribute names (ReqIF.Name, ReqIF.Text, ReqIF.ChapterName, ReqIF.ForeignID, ...), read-only attributes, embedded objects and attachments, XHTML usage, and tool interoperability. Use when the user asks what the implementation guide recommends, how tools are expected to exchange ReqIF in practice, or about a G-xxx finding from reqif-file-inspection.
---

# Implementation Guide citation

The prostep ivip ReqIF Implementation Guide is an informal companion to the OMG standard. It covers
implementation questions the standard leaves open. It is **not public**: this plugin never
downloads it, and its text exists only on this machine if the user imported their own copy.

`<plugin-root>` is the plugin root from the reqifsharp-sage SessionStart context (or two
directories above this skill's base directory).

## Read order

1. Check `<plugin-root>/knowledge/installed.json` → `guides[]`. If no guide has `generated: true`:
   - say the guide's text is not available;
   - if the user has a licensed copy, offer `import-guide` and then `generate` (see
     `knowledge-setup`);
   - never ask the user to paste the guide, and never reconstruct its contents from memory. You
     may say in general terms what topics it covers.
2. `<plugin-root>/knowledge/psi-guide/<g>/index.json` is an array of
   `{clause, title, pages, file, license: "prostep-ivip-restricted", redistributable: false}`.
3. Read `<plugin-root>/knowledge/psi-guide/<g>/<file>`. Paragraphs wrapped in
   `<!-- recommendation -->` are the guide's "should"-style recommendations.

## Rules

- Tag guide content **GUIDE**. It is informative: it is not part of the OMG standard and does not
  override it. When the guide and the standard both address a point, cite both, standard first
  (`reqif-spec-citation`).
- Give the reference as **"prostep ivip ReqIF Implementation Guide 1.7 §<clause> <title>"**.
- Keep quotes minimal and **never write guide text into files**: not into the repository, code
  comments, commit messages, issues or PRs. Use it to inform answers and paraphrase in anything
  written down. This matters most when working in public repositories such as reqifsharp.
- Some guide content is figures and tool screenshots, which the extraction does not reproduce.
