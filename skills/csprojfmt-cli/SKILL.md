---
name: csprojfmt-cli
description: Use the local csproj formatter CLI in this repository. Trigger when the user asks to format the current project file, format the current solution, run formatting recursively, run a dry run, or check whether formatting changes are needed.
---

# CsProjFmt CLI Skill

Use this skill to run the repository-local `csprojfmt` tool.

## Commands

Use PowerShell commands from the repository root.

```powershell
dotnet publish .\CsProjFormatter.Cli\CsProjFormatter.Cli.csproj -c Release -o .\artifacts\tools\csprojfmt --nologo
.\artifacts\tools\csprojfmt\csprojfmt.exe --version
```

If the executable is missing, publish first.

## Intent Mapping

Format the current project file:
1. Look for `*.csproj` files in the current working directory only.
2. If exactly one exists, format it.
3. If multiple exist, prefer `<current-folder-name>.csproj`.
4. If still ambiguous, ask the user which file to format.

```powershell
.\artifacts\tools\csprojfmt\csprojfmt.exe .\MyProject.csproj
```

Format the current solution:
1. Run from the solution root (or current repo root).
2. Format all project files recursively.

```powershell
.\artifacts\tools\csprojfmt\csprojfmt.exe -r .\
```

## Check And Preview Modes

Preview without writing:

```powershell
.\artifacts\tools\csprojfmt\csprojfmt.exe --dry-run -r .\
```

CI-style check (exit code `1` if changes would be required):

```powershell
.\artifacts\tools\csprojfmt\csprojfmt.exe --check -r .\
```

## Reporting

Always report:
1. Command used.
2. Per-file status lines (`updated`, `unchanged`, `skipped`, or `would-update`).
3. Final summary line from the tool.

`skipped` means the file was skipped because EditorConfig settings did not activate formatting for that file, or because the project file is not SDK-style.
