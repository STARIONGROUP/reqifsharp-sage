# hooks/native/

Committed, platform-specific `SessionStart` hook binaries: a small, dependency-free NativeAOT build
of `tools/sage-cli/ReqIFSharp.Sage.Tools.Hook`. At session start it finds the full `reqifsharp-sage` CLI - a build in this
checkout (`tools/sage-cli/ReqIFSharp.Sage.Tools/bin/{Release,Debug}/net10.0/`) if there is one,
otherwise the release pinned by `sageCliVersion` in `.claude-plugin/plugin.json`, from the
`%LOCALAPPDATA%/reqifsharp-sage/` cache - runs `check --json`, and tells
Claude the plugin root, the CLI's absolute path (it is not on PATH) and what still needs setting
up. It depends on no library (not ReqIFSharp, not System.CommandLine), which is what makes it
NativeAOT-publishable; the full CLI is not - see the root `CLAUDE.md`.

The session path never downloads anything. When the pinned CLI is not cached yet, the hook starts a
detached copy of itself (`reqifsharp-sage-hook --provision <plugin-root> <version>`) that downloads
the release from GitHub, verifies its SHA-256 against the release notes and unpacks it, with a
10-minute limit and a `<cache-dir>.provisioning` lock so concurrent sessions do not download it
twice. That session is told the CLI will be available next session (and how to use `dotnet run`
meanwhile).

## Layout

```
hooks/native/
├── win-x64/reqifsharp-sage-hook.exe
├── linux-x64/reqifsharp-sage-hook
└── osx-arm64/reqifsharp-sage-hook
```

`.claude-plugin/plugin.json`'s `SessionStart` hook command dispatches to the matching binary by
`uname -s`/`uname -m` at runtime; on any platform without a matching case (currently Intel Mac,
`Darwin-x86_64`) the hook simply no-ops rather than failing.

**No `osx-x64` build**: GitHub's `macos-13` hosted runner (the last Intel-Mac image) queued
indefinitely rather than starting, as of 2026-09 - consistent with GitHub retiring Intel Mac runners
in favor of Apple Silicon. Revisit if a hosted or self-hosted Intel-Mac runner becomes available
again; until then, Intel Mac users get no `SessionStart` status check (skills/agents still work
normally against whatever `knowledge/` tree exists on disk - the hook is a convenience, not a
dependency).

**No dispatch on native Windows without Git Bash**: `.claude-plugin/plugin.json`'s `SessionStart`
hook command is a POSIX `case`/`esac` script (`uname -s`/`uname -m` dispatch), so it needs a real
`bash` to run at all - it pins `"shell": "bash"` explicitly rather than relying on Claude Code's
unspecified-shell default (which, at least as of 2026-09, has been observed silently falling back to
PowerShell on Windows even with Git for Windows properly installed and `bash.exe` on `PATH` - the
exact default-resolution heuristic isn't publicly documented, so pinning `"shell": "bash"` is the
more reliable of the two). On Windows this needs [Git for
Windows](https://git-scm.com/downloads/win) (commonly already present on dev machines) so `bash` is
on `PATH`; with no `bash` anywhere - no Git Bash, no WSL - the hook fails to run (a harmless,
non-blocking "SessionStart:startup hook error" - skills/agents still work normally, same as the
Intel Mac case above).

There's no fix for that last case (no `bash` anywhere on Windows) beyond installing Git Bash: the
hook schema has no way to scope a hook entry to a specific OS/platform (`matcher` only matches
event-specific strings like `"startup|resume"`; `if` is tool-call permission syntax, not applicable
to `SessionStart`), and a single command string cannot be valid bash *and* valid PowerShell at once
(verified directly - PowerShell's `<# #>` block comments are fatal to bash's parser, bash's `<<`
heredocs are fatal to PowerShell's, and PowerShell requires the *entire* command to parse before
executing any of it, so there is no way to hide one language's syntax from the other within one
command). Adding a second, `"shell":"powershell"`-forced hook entry to cover this case would run
unconditionally on every platform, including POSIX machines that don't have `pwsh` installed by
default - trading a Windows-only, non-blocking annoyance for a new one on every other platform.

## How these are built

`.github/workflows/hook-binaries.yml` runs a build matrix across the three currently-buildable
runtime identifiers and commits the resulting binaries here as part of cutting a release. To build
one locally (including for `osx-x64`, on real Intel-Mac hardware):

```bash
dotnet publish tools/sage-cli/ReqIFSharp.Sage.Tools.Hook -r <rid> -c Release --self-contained -p:PublishAot=true -o hooks/native/<rid>
```

**Prerequisites for a NativeAOT publish**: the C++ linker toolchain for your platform (on Windows,
the "Desktop development with C++" Visual Studio workload; on Linux, `clang`; on macOS, Xcode
command line tools) - see https://aka.ms/nativeaot-prerequisites. Without it, `dotnet publish` fails
with "Platform linker not found" (this is a build-time-only requirement; the published binary itself
has no such dependency).

## Why binaries are committed at all

This is the one deliberate exception to "nothing generated is committed" (see the root `CLAUDE.md`,
"Committed vs git-ignored") - these binaries contain no OMG-derived content whatsoever (they're
compiled from this repository's own C# source, `tools/sage-cli/ReqIFSharp.Sage.Tools.Hook/`), and a
plugin install needs a ready-to-run hook without requiring the end user to have a .NET SDK and a
C++ linker toolchain installed just to use the plugin at all.
