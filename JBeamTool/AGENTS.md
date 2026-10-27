# JBeamTool Agent Instructions

This repo contains a CLI for BeamNG JBeam validation and inspection. Use it as the source of truth for JBeam file checks. Do not rely on manual text inspection when the CLI can answer the same question with structured output.

## Best execution rule

Always invoke the real binary path from the repo root or via a wrapper script. Do not assume `JBeamTool.exe` is on PATH.

Preferred repo-local pattern:

```powershell
cd "C:\Users\MrX13\Projekte\BeamNG - VWBug\Source\JBeamTool"
& ".\JBeamTool\bin\Debug\net10.0\JBeamTool.exe" verify "C:\path\to\file.jbeam" --json --strict
```

If you need a shorter wrapper, use a repo-local script in a `build` folder. The wrapper should resolve the binary path for you and always pass `--json --strict`.

## Required workflow for every JBeam edit

1. Run `verify` before editing.
2. Make the change.
3. Run `verify` again after editing.
4. Treat warnings as failures in strict mode.
5. Use `info` or `ast` only for inspection, not as the primary validation step.

## Commands

### Verify a JBeam file or directory

```bash
JBeamTool.exe verify <path> --json --strict
```

Use this before and after every JBeam edit.

- `--json` makes the result automation-safe.
- `--strict` turns warnings into a failed result.
- This command reports: file count, warnings, sanity status, part counts, and file-level pass/fail.

Examples:

```powershell
& ".\JBeamTool\bin\Debug\net10.0\JBeamTool.exe" verify "C:\mods\vehicle\parts" --json --strict
& ".\JBeamTool\bin\Debug\net10.0\JBeamTool.exe" verify "C:\mods\vehicle\part.jbeam" --json --strict
```

### Inspect a single JBeam file

```bash
JBeamTool.exe info <file.jbeam> --json
```

Use this when you need a compact summary of a single JBeam file without reading the raw source text.

It returns:

- file path
- loaded status
- part count
- sanity status
- warnings
- section counts
- per-part metadata

This command is intentionally single-file oriented for compact output.

### Expose the parsed JBeam AST

```bash
JBeamTool.exe ast <path> --json
```

Use this when you need the parsed structure and nested nodes directly, without manually interpreting the raw JBeam syntax.

This is the best command for a tool or AI that wants to reason about the actual parsed object graph.

## Interpretation

- `ok: true` means the file or set of files passed validation.
- `ok: false` means the command found a problem or warnings under strict mode.
- `sanity: false` means the parser detected a top-level structural inconsistency.
- Do not treat warnings as harmless if the command was run in strict mode; in that mode any warning counts as failure.
- Prefer `--json` for any agent-facing workflow. Human-readable output is fine for local debugging, but not as the source of truth for automation.

## Wrapper script recommendation

For a smoother workflow, create a small `build` helper script, for example:

```powershell
# build/jbeam-verify.ps1
$root = Split-Path -Parent $PSScriptRoot
$binary = Join-Path $root "JBeamTool\bin\Debug\net10.0\JBeamTool.exe"
& $binary verify $args --json --strict
```

Then the AI workflow becomes:

```powershell
./build/jbeam-verify.ps1 "C:\path\to\part.jbeam"
```

This removes path confusion and keeps the execution contract consistent.

## Decision

- Use `JBeamTool/JBeamTool/AGENTS.md` as the project-specific CLI rule file.
- Use a local wrapper script only if the binary path is inconvenient.
- A generic global skill is not necessary for a single tool and repo; the AGENTS file + wrapper script is cleaner and more reliable.

## Safe behavior

- Always validate before and after any JBeam edit.
- Always prefer the CLI result over manual reading when making a change.
- Use `--strict` as the default for safety-critical validation steps.
