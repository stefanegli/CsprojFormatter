---
name: csprojfmt
description: Use the PetchNaka.CsProjFormatter.Tool `csprojfmt` .NET global tool to check, preview, and format SDK-style `.csproj` files according to EditorConfig; help install or update the tool; interpret statuses and exit codes; configure formatting rules; integrate checks into CI; or diagnose skipped, unchanged, and failed files. Use when Codex needs to operate `csprojfmt` on PATH, prepare the required .NET tool, or work in the CsProjFormatter source repository.
---

# CsProjFormatter CLI

Use `csprojfmt` conservatively: preview first, preserve the user's EditorConfig policy, apply only requested changes, and verify the result.

## Workflow

1. Inspect the target paths, repository status, and applicable `.editorconfig` before writing.
2. Resolve the command:
   - Prefer an explicit `csprojfmt` executable or `csprojfmt` already on `PATH`.
   - If it is unavailable, inform the user that the `PetchNaka.CsProjFormatter.Tool` .NET global tool is required and that the .NET 10 SDK is needed to install it. Offer to install it and obtain permission before changing the user's global tool configuration.
   - When authorized, run `dotnet tool install --global PetchNaka.CsProjFormatter.Tool`, then verify with `csprojfmt --version`.
   - If the package is already installed but needs updating, offer `dotnet tool update --global PetchNaka.CsProjFormatter.Tool`; do not update it silently.
   - In the CsProjFormatter source repository, `dotnet run --project CsProjFormatter.Cli/CsProjFormatter.Cli.csproj -- <arguments>` is an acceptable development fallback.
3. Preview the exact scope with `--check`. Add `--recursive` only when nested directories belong in scope.
4. Interpret exit code `1` from `--check` as pending formatting changes, not an execution failure. Treat exit code `2` as a usage, path, access, or formatting failure.
5. Stop after the preview when the user requested a check, audit, or dry run.
6. When the user requested formatting, rerun the same targets and recursion choice without `--check` or `--dry-run`.
7. Verify the write by rerunning `--check` on the identical scope. Inspect `git diff --stat` and the relevant diff when working in a repository.
8. Report updated, unchanged, skipped, and failed results. Clearly identify any files left unformatted.

## Guardrails

- Require the .NET 10 SDK to install the tool; it also supplies the runtime required by the tool.
- Do not install or update a global tool without the user's approval.
- If a newly installed global tool is not visible on `PATH`, use the standard .NET global-tools directory for the current platform or explain that the shell may need to be restarted.
- Do not add or change `.editorconfig` merely to make a skipped file active unless the user requested configuration changes. Explain the missing policy instead.
- Do not assume directory arguments recurse; recursion requires `--recursive`.
- Use `--` before a dash-prefixed path.
- Keep explicit file and directory arguments intact between preview, write, and verification.
- Use `--verbose` to diagnose malformed files or EditorConfig parsing, but expect more detailed output and no compact summary.
- Avoid formatting unrelated project files in a dirty worktree.
- Do not hand-edit project-file ordering or XML layout when the CLI can apply the repository's configured policy.

## Reference

Read [references/cli-reference.md](references/cli-reference.md) for installation and update commands, exact CLI usage, settings, output statuses, exit codes, source-repository usage, and CI patterns.
