# CsProjFormatter
Optimizies resx files after saving: Removes comments (in particular the 3KB documentation that is included in every resx file) and sorts entries alphabetically. Use only with a source control system and at your own risk.

See the [change log](CHANGELOG.md) for changes and road map.

----
Download this extension from the [Visual Studio Marketplace](https://marketplace.visualstudio.com/items?itemName=stefan-egli.CsProjFormatter)
or get the [CI build](http://vsixgallery.com/extension/CsProjFormatter.C4817943-A856-49E2-A982-5FC24F7ABE7C/).



[![Build status](https://ci.appveyor.com/api/projects/status/3fn0a5uhraovv6a3?svg=true)](https://ci.appveyor.com/project/stefanegli/CsProjFormatter)


# Settings

## EditorConfig
Formatting rules are configured in the [EditorConfig](https://editorconfig.org/) file as follows:

```ini
[*.csproj]
csproj_formatter_sort_entries=true
csproj_formatter_empty_lines_between_groups=1
indent_style=space
tab_width=4
end_of_line=crlf
```

Sorting behavior:
- PropertyGroup entries are sorted alphabetically, but dependencies like `$(Version)` are kept before the properties that reference them.
- ItemGroup entries are sorted when all items are the same type: `PackageReference`, `ProjectReference`, `Reference`, or `None`.
- PackageReference groups are ordered as: normal packages, `IncludeAssets`, `PrivateAssets`, and then `Condition`, with alphabetical sorting inside each group.
- Top-level groups are separated by one empty line by default. Configure with `csproj_formatter_empty_lines_between_groups` (`0` disables extra blank lines).

A few things can be configured and probably you want to have this done as follows:

![Settings](CsProjFormatter/_doc/Settings.png)

> Use the experimental setting with caution since it may have undesired side effects.

# CLI
The repository includes a console app named `csprojfmt` that applies the same formatting rules as the VS extension.
It targets .NET 10.

Publish:
```
dotnet publish CsProjFormatter.Cli/CsProjFormatter.Cli.csproj -c Release -o ./artifacts/tools/csprojfmt --nologo
```
Publish output:
- `artifacts/tools/csprojfmt/csprojfmt.exe`

Usage:
```
csprojfmt [options] [<path> ...]
```

Options:
- `-r`, `--recursive` Recurse into subdirectories when a path is a directory.
- `-v`, `--verbose` Show per-file status and errors.
- `-n`, `--dry-run` Show what would change without writing files.
- `--check` Exit with code 1 if any file would change (implies `--dry-run`).

Output:
- Prints one line per file with a status (`updated`, `unchanged`, `skipped`).
- Paths are shown relative to the current working directory.
- `skipped` means formatting is disabled by EditorConfig or the file is not an SDK-style project.

Default path behavior:
- If no path is provided, the current directory is processed.


# Contributing
Please use the [issue tracker](https://github.com/stefanegli/CsProjFormatter/issues) for submitting bug reports or feature requests.

# License
[MIT License](LICENSE)
