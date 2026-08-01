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

- PropertyGroup entries are sorted alphabetically when doing so is evaluation-safe. The relative order of properties that assign or reference the same `$(Property)` chain is preserved.
- ItemGroup entries are sorted only when the group contains one configured item type, every item is include-only, and identities are unique. Groups using `Update`, `Remove`, item expressions, or duplicate identities retain their original order.
- The built-in item types cover common .NET, desktop, compiler, packaging, and SDK extensibility items, including `Compile`, `Content`, `EmbeddedResource`, `None`, `PackageReference`, `PackageDownload`, `ProjectReference`, `FrameworkReference`, `Reference`, `Analyzer`, `AdditionalFiles`, `EditorConfigFiles`, `Page`, `ApplicationDefinition`, `Resource`, `Using`, `AssemblyAttribute`, and `InternalsVisibleTo`.
- Override the built-in list with `csproj_formatter_sort_item_types`. The value is a case-insensitive replacement list, not an addition to the defaults. Separate names with commas or semicolons, or use `*` to allow any homogeneous item type.
- Item attributes and child metadata use a stable canonical order: identity operation first, commonly used metadata next, unknown names alphabetically, and `Condition` last. Metadata references such as `%(Filename)` retain evaluation-safe ordering.
- Top-level groups are separated by one empty line by default. Configure with `csproj_formatter_empty_lines_between_groups` (`0` disables extra blank lines).

For example, this policy makes only `PackageReference` and `Protobuf` items eligible for item sorting and canonicalization:

```ini
[*.csproj]
csproj_formatter_sort_entries=true
csproj_formatter_sort_item_types=PackageReference, Protobuf
```

All other item types retain their original item, attribute, and child-metadata order. XML indentation and spacing still apply, and `--lint` still inspects them. Omit `csproj_formatter_sort_item_types` to use the built-in list. To retain every built-in type while adding a custom type, copy the current `defaultValue` from [the EditorConfig schema](CsProjFormatter/CsProjFormatter.editorconfig-schema.json) and append the custom name. Use `*` only when every item type should be eligible; the formatter's homogeneous-group and evaluation-safety checks still apply.


# Agent Skill / CLI

Download `csprojfmt-<version>.zip` from an [AppVeyor build](https://ci.appveyor.com/project/stefanegli/csprojformatter) and extract its `csprojfmt` directory into your Codex skills directory. The skill uses the `PetchNaka.CsProjFormatter.Cli` .NET global tool and does not bundle an executable.

Invoke the agent skill with:

```text
Use $csprojfmt to check and format the .csproj files in this repository.
```

The agent will detect when the tool is missing and can help install it. To install it directly, first install the .NET 10 SDK and then run:

```powershell
dotnet tool install --global PetchNaka.CsProjFormatter.Cli
```

Update an existing installation with:

```powershell
dotnet tool update --global PetchNaka.CsProjFormatter.Cli
```

Run `csprojfmt` from the directory containing the project files you want to process. The command syntax is `csprojfmt [options] [<path> ...]`. Use `--check` to detect required changes without writing, `--lint` to report structural diagnostics as well as formatting changes, `--dry-run` to preview, `--recursive` to include subdirectories, and `--verbose` for detailed output. Recursive discovery skips common generated directories such as `bin`, `obj`, `.git`, `.vs`, `artifacts`, and `node_modules`. Formatting follows the applicable EditorConfig settings; without a path, the current directory is processed.

`--lint` works even when no formatter-specific EditorConfig setting is present and never writes files. It reports empty or mixed groups, duplicate items, conflicting target-framework properties, explicit items that may duplicate .NET SDK defaults, and unexpected top-level elements. Diagnostics use stable codes `CSPROJ001` through `CSPROJ006` and include source line numbers when available.

The CLI reports `updated`, `would-update`, `unchanged`, `skipped`, or `failed` for each file. A skipped file either has no applicable formatting settings or is not an SDK-style project. Exit code `1` means `--check` found pending changes or `--lint` found a diagnostic; exit code `2` means a usage, path, access, or formatting failure.

# Contributing
Please use the [issue tracker](https://github.com/stefanegli/CsProjFormatter/issues) for submitting bug reports or feature requests.

# License
[MIT License](LICENSE)

## Third Party Licenses

| Library | License |
| ------- | ------- |
| [EditorConfig .NET Core](https://github.com/editorconfig/editorconfig-core-net) | [MIT License](https://github.com/editorconfig/editorconfig-core-net/blob/master/LICENSE) |
| [xUnit](https://github.com/xunit/xunit) | [Apache License 2.0 / MIT License](https://github.com/xunit/xunit/blob/main/LICENSE) |
| [NFluent](https://github.com/tpierrain/NFluent) | [Apache License 2.0](https://github.com/tpierrain/NFluent/blob/master/LICENSE.txt) |
