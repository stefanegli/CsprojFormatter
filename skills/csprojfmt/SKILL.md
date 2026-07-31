---
name: csprojfmt
description: Check, preview, format, diagnose, configure, or enforce EditorConfig-based formatting for SDK-style `.csproj` files with the `csprojfmt` .NET global tool. Use when creating or updating CsProjFormatter policy, including sortable item types. Also use to install or update `PetchNaka.CsProjFormatter.Cli`, interpret its results, or run it from source.
---

# CsProjFormatter CLI

Preview before writing, preserve the applicable EditorConfig policy, and verify changes.

## Workflow

1. Inspect the targets, repository status, and applicable `.editorconfig`.
2. Before creating or changing CsProjFormatter settings, read the EditorConfig policy section in [references/cli-reference.md](references/cli-reference.md). Preserve the existing policy unless the user requests a change.
3. Use `csprojfmt` on `PATH`. If missing, explain that the .NET 10 SDK and `PetchNaka.CsProjFormatter.Cli` are required. Obtain permission before installing or updating the global tool, then verify with `csprojfmt --version`.
4. Preview the exact scope with `--check`; use `--lint` when structural diagnostics are requested. Add `--recursive` only when requested or clearly intended.
5. For checks, lints, or audits, stop and report the preview. For formatting, rerun the same scope without `--check`, then verify it with `--check` and inspect the relevant diff.
6. Treat exit `1` from `--check` as pending changes and from `--lint` as pending changes or diagnostics. Treat exit `2` as an execution failure. Report updated, unchanged, skipped, failed, and diagnostic results.

## Guardrails

- Do not install or update globally without approval.
- Do not change `.editorconfig` merely to activate a skipped file.
- Treat `csproj_formatter_sort_item_types` as a replacement list. When the user asks to add a type while retaining defaults, include all built-in types plus the addition; do not replace the defaults with only the new type.
- Do not use `*` unless the user intends every item type to be eligible for sorting and canonicalization.
- Keep targets and recursion identical between preview, write, and verification.
- Avoid formatting unrelated project files in a dirty worktree.
- In this source repository, use `dotnet run --project CsProjFormatter.Cli/CsProjFormatter.Cli.csproj -- <arguments>` when the checked-out code is required.

## Details

Read [references/cli-reference.md](references/cli-reference.md) when exact commands, options, settings, statuses, exit codes, PATH troubleshooting, or CI examples are needed.
