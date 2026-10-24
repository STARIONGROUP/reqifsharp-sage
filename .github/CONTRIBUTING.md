# How to contribute

Thank you for wanting to contribute to reqifsharp-sage. A few guidelines help us keep on top of
things.

## Making changes

1. [Fork](https://help.github.com/forking/) the repository on GitHub and clone your fork.
1. Add the upstream remote: `git remote add upstream https://github.com/STARIONGROUP/reqifsharp-sage.git`.
1. Check out `development` and create a branch from it (`git checkout -b my-branch`).
1. Make your change, with tests.
1. Rebase on `upstream/development` before you open the pull request:
   `git fetch upstream && git rebase upstream/development`.
1. Push the branch and open a pull request against `development`.

Never work on, or send a pull request from, `master` or `development` directly.

## Licensed sources - please read

This repository must never contain OMG specification material beyond what `NOTICE` reproduces,
and never any text of the prostep ivip ReqIF Implementation Guide, which is not public. This
applies to test fixtures, comments, rule messages, documentation, issues and pull requests too.
`sources/` and `knowledge/` are git-ignored for this reason. See `CLAUDE.md`, "Committed vs
git-ignored".

## Building and testing

```bash
dotnet build reqifsharp-sage.slnx
dotnet test reqifsharp-sage.slnx

cd tools/spec-extract
python -m venv .venv && .venv/Scripts/pip install -e ".[dev]"
.venv/Scripts/pytest -v && .venv/Scripts/ruff check . && .venv/Scripts/ruff format --check .
```

## Style

Follow the conventions in `CLAUDE.md` (also enforced by `.editorconfig`): an SPDX (Apache-2.0) header on
every `.cs` file, `this.`, braces everywhere, `using`s inside the namespace, and XML docs on public
members.

**A pull request with tests is a pull request that is likely to be merged.**
