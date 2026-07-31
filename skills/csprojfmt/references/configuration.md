# EditorConfig reference

## Basic policy

```ini
[*.csproj]
csproj_formatter_sort_entries=true
csproj_formatter_empty_lines_between_groups=1
indent_style=space
tab_width=4
end_of_line=crlf
```

- `csproj_formatter_sort_entries=true` enables property and item sorting plus item attribute/metadata canonicalization.
- `csproj_formatter_empty_lines_between_groups` accepts a non-negative integer; `0` disables extra blank lines.
- Standard `indent_style`, `tab_width`, and `end_of_line` settings control XML layout.
- Formatting activates when any supported formatter or layout setting applies. Preserve the repository's existing policy instead of inserting the example blindly.

## Item-type policy

`csproj_formatter_sort_item_types` is case-insensitive and replaces the built-in list; it never extends it implicitly.

| Configuration | Effect |
| --- | --- |
| Setting omitted | Use the built-in list below. |
| Non-empty name list | Only listed types are eligible for item sorting and attribute/metadata canonicalization. |
| `*` | Make every item type eligible. |
| Item type not listed | Preserve its item, attribute, and child-metadata order. XML layout and linting still apply. |

Separate names with commas or semicolons. Do not use an empty value to disable item sorting. Set `csproj_formatter_sort_entries=false` to disable all entry sorting and item canonicalization.

Built-in types:

```text
AdditionalFiles, Analyzer, ApplicationDefinition, AssemblyAttribute, AssemblyMetadata, Compile, CompilerVisibleItemMetadata, CompilerVisibleProperty, COMFileReference, COMReference, Content, EditorConfigFiles, EmbeddedResource, Folder, FrameworkReference, GlobalAnalyzerConfigFiles, InternalsVisibleTo, NativeReference, None, PackageDownload, PackageReference, Page, ProjectReference, PrunePackageReference, Reference, Resource, RuntimeHostConfigurationOption, SplashScreen, TrimmerRootAssembly, Using
```

To retain defaults while adding a custom type, copy the complete list into the setting and append the new name. Inspect the project item types and current EditorConfig value first. Use `*` only when custom and future item types should also be eligible.

Eligibility does not guarantee reordering. A sortable group must be homogeneous, include-only, and have unique identities. Groups using `Update`, `Remove`, item expressions, or duplicate identities retain item order. Eligible items can still have attributes and metadata canonicalized when evaluation-safe.

Only SDK-style projects are formatted.
