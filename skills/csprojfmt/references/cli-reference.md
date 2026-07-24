# `csprojfmt` CLI reference

## Command

```text
csprojfmt [options] [<path> ...]
```

With no path, process the current directory. A directory is non-recursive unless `--recursive` is present. Accept only `.csproj` file targets; deduplicate overlapping targets.

The packaged `$csprojfmt` skill stores two framework-dependent single-file executables:

```text
<skill-directory>\assets\cli\win-x64\csprojfmt.exe
<skill-directory>/assets/cli/linux-x64/csprojfmt
```

Select the executable matching the operating system and x64 architecture. Resolve it to an absolute path, then invoke it while the current working directory is the repository containing the target files.

On Linux, restore the executable bit after ZIP extraction when necessary:

```bash
chmod u+x <skill-directory>/assets/cli/linux-x64/csprojfmt
```

Both executables require the .NET 10 runtime. They are single-file and framework-dependent, with ReadyToRun disabled. A single executable cannot target both Windows and Linux because .NET single-file apps are runtime-identifier-specific.

When running from the CsProjFormatter source repository without a directly invocable executable:

```powershell
dotnet run --project CsProjFormatter.Cli/CsProjFormatter.Cli.csproj -- --check --recursive .
```

Build or publish from the repository root with the .NET 10 SDK:

```powershell
dotnet build CsProjFormatter.Cli/CsProjFormatter.Cli.csproj --nologo
dotnet publish CsProjFormatter.Cli/CsProjFormatter.Cli.csproj -c Release --nologo
```

Prefer the matching packaged executable. Prefer `dotnet run` over guessing a repository artifact path when the operating system, architecture, build configuration, or platform differs.

## Options

| Option | Effect |
| --- | --- |
| `-r`, `--recursive` | Recurse into directory targets. |
| `-v`, `--verbose` | Show detailed per-file logging and exception details. |
| `-n`, `--dry-run` | Preview without writing; return `0` even when changes are pending. |
| `--check` | Preview without writing; return `1` when any active file would change. |
| `-h`, `--help`, `/?` | Print help and return `0`. |
| `-V`, `--version` | Print the version and return `0`. |
| `--` | Stop option parsing so dash-prefixed paths can be targeted. |

## Safe command patterns

Check one file:

```powershell
csprojfmt --check .\src\MyProject.csproj
```

Check every `.csproj` file beneath the current directory:

```powershell
csprojfmt --check --recursive .
```

Preview changes without making pending changes fail the command:

```powershell
csprojfmt --dry-run --recursive .
```

Format and then verify the same scope:

```powershell
csprojfmt --recursive .
csprojfmt --check --recursive .
```

Target a dash-prefixed file:

```powershell
csprojfmt -- --input.csproj
```

## EditorConfig policy

Formatting uses the applicable EditorConfig settings. A typical policy is:

```ini
[*.csproj]
csproj_formatter_sort_entries=true
csproj_formatter_empty_lines_between_groups=1
indent_style=space
tab_width=4
end_of_line=crlf
```

`csproj_formatter_sort_entries` sorts supported property and item groups when set to `true`. `csproj_formatter_empty_lines_between_groups` accepts a non-negative integer; `0` disables extra blank lines. Standard `indent_style`, `tab_width`, and `end_of_line` settings control XML layout.

Formatting is active only when a supported formatter or layout setting applies to the target file. Preserve the repository's intended ordering, indentation, line-ending, and spacing policy instead of inserting the full example blindly.

Only SDK-style projects are formatted. Non-SDK-style `.csproj` files are reported as skipped.

## Statuses

| Status | Meaning |
| --- | --- |
| `updated` | The file changed on disk. |
| `would-update` | The active file would change, but dry-run/check prevented a write. |
| `unchanged` | Formatting is active and the file already matches policy. |
| `skipped` | No supported setting applies, or the file is not an SDK-style project. |
| `failed` | The file could not be formatted, commonly because it is malformed or inaccessible. |

Paths in output are relative to the current working directory when possible. Without `--verbose`, the CLI also prints a compact summary. With `--verbose`, rely on per-file output and diagnostics.

## Exit codes

| Code | Meaning |
| --- | --- |
| `0` | Successful execution; with `--check`, no active file needs changes. |
| `1` | `--check` found at least one file that would change. |
| `2` | Unknown option, invalid/non-`.csproj`/missing path, access error, or formatting failure. |

`No .csproj files found.` can return `0` when the searched scope is valid but empty, or `2` when path errors also occurred. Inspect standard error before treating an empty result as success.

## CI

Use `--check --recursive` to enforce formatting without modifying the checkout:

```powershell
csprojfmt --check --recursive .
```

Treat exit `1` as a formatting-policy violation and exit `2` as an execution/configuration failure. Preserve CLI output in the job log so pending or failed paths are visible.
