# CsProjFormatter
Formats SDK-style .csproj files after saving: sorts supported entries, normalizes XML formatting, and applies consistent spacing. Use only with a source control system and at your own risk.
Targets Visual Studio 2022 and newer.

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

When the [EditorConfig Language Service](https://github.com/madskristensen/EditorConfigLanguage) version 1.18.35 or newer is installed, the CsProjFormatter VSIX contributes these properties to its IntelliSense and validation. Restart Visual Studio after installing or updating either extension so that the custom schema is loaded.

Sorting behavior:

- PropertyGroup entries are sorted alphabetically, but dependencies like `$(Version)` are kept before the properties that reference them.
- ItemGroup entries are sorted when all items are the same type: `PackageReference`, `ProjectReference`, `Reference`, or `None`.
- PackageReference groups are ordered as: normal packages, `IncludeAssets`, `PrivateAssets`, and then `Condition`, with alphabetical sorting inside each group.
- Top-level groups are separated by one empty line by default. Configure with `csproj_formatter_empty_lines_between_groups` (`0` disables extra blank lines).

A few things can be configured and probably you want to have this done as follows:

![Settings](CsProjFormatter/_doc/Settings.png)

> Use the experimental setting with caution since it may have undesired side effects.

# Agent Skill / CLI

Download `csprojfmt-<version>.zip` from an [AppVeyor build](https://ci.appveyor.com/project/stefanegli/csprojformatter) and extract its `csprojfmt` directory into your Codex skills directory. The skill includes .NET 10 framework-dependent single-file executables for Windows x64 and Linux x64.

> [!NOTE]
> The packaged executables are not digitally signed.

Invoke the agent skill with:

```text
Use $csprojfmt to check and format the .csproj files in this repository.
```

Or call the packaged executable directly from the directory containing the project files you want to process.

Windows:

```powershell
& "$env:USERPROFILE\.codex\skills\csprojfmt\assets\cli\win-x64\csprojfmt.exe" --check --recursive .
```

Linux:

```bash
chmod u+x "$HOME/.codex/skills/csprojfmt/assets/cli/linux-x64/csprojfmt"
"$HOME/.codex/skills/csprojfmt/assets/cli/linux-x64/csprojfmt" --check --recursive .
```

The command syntax is `csprojfmt [options] [<path> ...]`. Use `--check` to detect required changes without writing, `--dry-run` to preview, `--recursive` to include subdirectories, and `--verbose` for detailed output. Formatting follows the applicable EditorConfig settings; without a path, the current directory is processed.

The CLI reports `updated`, `would-update`, `unchanged`, `skipped`, or `failed` for each file. A skipped file either has no applicable formatting settings or is not an SDK-style project. Exit code `1` means `--check` found pending changes; exit code `2` means a usage, path, access, or formatting failure.

# Contributing
Please use the [issue tracker](https://github.com/stefanegli/CsProjFormatter/issues) for submitting bug reports or feature requests.

# License
[MIT License](LICENSE)
