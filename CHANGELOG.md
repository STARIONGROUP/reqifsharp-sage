# Changelog

All notable changes to this project are documented here. The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the project adheres to
[Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added

- Claude Code plugin with the skills `knowledge-setup`, `metamodel-lookup`, `reqif-schema-lookup`,
  `reqif-spec-citation`, `implementation-guide-citation`, `reqifsharp-api-lookup`,
  `reqifsharp-usage` and `reqif-file-inspection`; the agents `metamodel-navigator`,
  `spec-citation` and `reqif-inspector`; and the `/overview` command.
- `reqifsharp-sage` CLI with the verbs `fetch` (omg.org or `--from` a local folder, matched by
  SHA-256), `import-guide`, `generate`, `versions`, `use`, `remove`, `check` and `inspect`.
- Knowledge generation for ReqIF 1.2:
  - the CMOF metamodel joined with the XML schema, giving per-feature XML bindings;
  - schema type pages and the XHTML subset;
  - specification clause text;
  - prostep ivip Implementation Guide clause text, from the user's own copy;
  - the reqifsharp API reference, with a ReqIF ↔ C# ↔ XML mapping.
- NativeAOT SessionStart hook that reports the CLI path and plugin root, and prefers a CLI built
  in the checkout over a released one.
- The hook downloads a missing CLI in a detached background process rather than within the session's
  start-up budget.
