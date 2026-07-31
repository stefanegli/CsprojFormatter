---
name: csprojfmt
description: Check, lint, configure, format, or enforce EditorConfig policy for SDK-style `.csproj` files with the `csprojfmt` .NET tool. Use for CsProjFormatter settings, sortable item types, tool installation or updates, result interpretation, and source-repository runs.
---

# CsProjFormatter CLI

Inspect first, preview before writing, preserve policy, and verify changes.

## Workflow

1. Inspect the targets, repository status, and applicable `.editorconfig`.
2. Before changing settings, read [references/configuration.md](references/configuration.md). Preserve existing policy. Remember that `csproj_formatter_sort_item_types` replaces the defaults; retain all defaults explicitly when adding a type.
3. Use `csprojfmt` on `PATH`. If missing, explain that the .NET 10 SDK and `PetchNaka.CsProjFormatter.Cli` are required. Obtain permission before installing or updating the global tool, then verify with `csprojfmt --version`.
4. Preview the exact scope with `--check`; use `--lint` for structural diagnostics. Add `--recursive` only when requested or clearly intended.
5. For formatting, rerun the same scope without `--check`, verify with `--check`, and inspect the relevant diff. For checks or lints, report without writing.
6. Treat exit `1` as findings (`--check` changes or `--lint` diagnostics), and exit `2` as execution failure.

## Guardrails

- Do not install or update globally without approval.
- Do not change `.editorconfig` merely to activate a skipped file.
- Do not use `*` unless the user intends every item type to be eligible for sorting and canonicalization.
- Keep targets and recursion identical between preview, write, and verification.
- Avoid formatting unrelated project files in a dirty worktree.
- In this source repository, use `dotnet run --project CsProjFormatter.Cli/CsProjFormatter.Cli.csproj -- <arguments>` when the checked-out code is required.

## References

- Read [references/configuration.md](references/configuration.md) for EditorConfig semantics, defaults, and safe item-type customization.
- Read [references/cli-reference.md](references/cli-reference.md) for commands, options, statuses, diagnostics, exit codes, installation, PATH troubleshooting, and CI.
