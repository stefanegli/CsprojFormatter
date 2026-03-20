using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CsProjFormatter;

namespace CsProjFormatter.Cli
{
    internal static class Program
    {
        private const string ToolName = "csprojfmt";

        public static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                PrintUsage(Console.Out);
                return 1;
            }

            var recursive = false;
            var verbose = false;
            var dryRun = false;
            var check = false;
            var stopOptions = false;
            var paths = new List<string>();

            foreach (var arg in args)
            {
                if (!stopOptions && string.Equals(arg, "--", StringComparison.Ordinal))
                {
                    stopOptions = true;
                    continue;
                }

                if (!stopOptions && IsHelpArg(arg))
                {
                    PrintUsage(Console.Out);
                    return 0;
                }

                if (!stopOptions && IsVersionArg(arg))
                {
                    PrintVersion();
                    return 0;
                }

                if (!stopOptions && (string.Equals(arg, "-r", StringComparison.Ordinal) || string.Equals(arg, "--recursive", StringComparison.Ordinal)))
                {
                    recursive = true;
                    continue;
                }

                if (!stopOptions && (string.Equals(arg, "-v", StringComparison.Ordinal) || string.Equals(arg, "--verbose", StringComparison.Ordinal)))
                {
                    verbose = true;
                    continue;
                }

                if (!stopOptions && (string.Equals(arg, "-n", StringComparison.Ordinal) || string.Equals(arg, "--dry-run", StringComparison.Ordinal)))
                {
                    dryRun = true;
                    continue;
                }

                if (!stopOptions && string.Equals(arg, "--check", StringComparison.Ordinal))
                {
                    check = true;
                    dryRun = true;
                    continue;
                }

                if (!stopOptions && arg.StartsWith("-", StringComparison.Ordinal))
                {
                    Console.Error.WriteLine($"Unknown option: {arg}");
                    PrintUsage(Console.Error);
                    return 2;
                }

                paths.Add(arg);
            }

            if (paths.Count == 0)
            {
                Console.Error.WriteLine("No input paths provided.");
                PrintUsage(Console.Error);
                return 2;
            }

            var pathErrors = new List<string>();
            var files = ResolveTargetFiles(paths, recursive, pathErrors);

            foreach (var error in pathErrors)
            {
                Console.Error.WriteLine(error);
            }

            if (files.Count == 0)
            {
                Console.WriteLine("No .csproj files found.");
                return pathErrors.Count > 0 ? 2 : 0;
            }

            var log = new ConsoleLog(verbose);
            var changed = 0;
            var unchanged = 0;
            var skipped = 0;
            var failed = 0;

            foreach (var file in files)
            {
                try
                {
                    var formatter = new ConfigurableCsProjFormatter(log);
                    formatter.Run(file, !dryRun);

                    if (!formatter.IsActive)
                    {
                        skipped++;
                        if (verbose)
                        {
                            Console.WriteLine($"Skipped (EditorConfig inactive): {file}");
                        }

                        continue;
                    }

                    if (formatter.IsFileChanged)
                    {
                        changed++;
                    }
                    else
                    {
                        unchanged++;
                        if (verbose)
                        {
                            Console.WriteLine($"No changes: {file}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    failed++;
                    Console.Error.WriteLine($"Failed to format {file}: {ex.Message}");
                    if (verbose)
                    {
                        Console.Error.WriteLine(ex);
                    }
                }
            }

            if (!verbose)
            {
                var changeLabel = dryRun ? "Would update" : "Updated";
                Console.WriteLine(
                    $"Processed {files.Count} file(s). {changeLabel} {changed}, unchanged {unchanged}, skipped {skipped}, failed {failed}.");
            }

            if (failed > 0)
            {
                return 2;
            }

            if (check && changed > 0)
            {
                return 1;
            }

            return 0;
        }

        private static bool IsHelpArg(string arg)
        {
            return string.Equals(arg, "-h", StringComparison.Ordinal)
                || string.Equals(arg, "--help", StringComparison.Ordinal)
                || string.Equals(arg, "/?", StringComparison.Ordinal);
        }

        private static bool IsVersionArg(string arg)
        {
            return string.Equals(arg, "-V", StringComparison.Ordinal)
                || string.Equals(arg, "--version", StringComparison.Ordinal);
        }

        private static void PrintVersion()
        {
            var version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown";
            Console.WriteLine($"{ToolName} {version}");
        }

        private static void PrintUsage(TextWriter writer)
        {
            writer.WriteLine($"Usage: {ToolName} [options] <path> [<path> ...]");
            writer.WriteLine();
            writer.WriteLine("Options:");
            writer.WriteLine("  -r, --recursive   Recurse into subdirectories when a path is a directory.");
            writer.WriteLine("  -v, --verbose     Show per-file status and errors.");
            writer.WriteLine("  -n, --dry-run     Show what would change without writing files.");
            writer.WriteLine("      --check       Exit with code 1 if any file would change (implies --dry-run).");
            writer.WriteLine("  -h, --help        Show this help.");
            writer.WriteLine("  -V, --version     Show version info.");
            writer.WriteLine();
            writer.WriteLine("Notes:");
            writer.WriteLine("  Formatting only runs when EditorConfig enables it (same as the VS extension)."
                + " Add csproj_formatter_sort_entries=true to your .editorconfig.");
        }

        private static List<string> ResolveTargetFiles(IEnumerable<string> paths, bool recursive, List<string> errors)
        {
            var results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            foreach (var rawPath in paths)
            {
                if (string.IsNullOrWhiteSpace(rawPath))
                {
                    continue;
                }

                var fullPath = Path.GetFullPath(rawPath);
                if (File.Exists(fullPath))
                {
                    if (IsCsproj(fullPath))
                    {
                        results.Add(fullPath);
                    }
                    else
                    {
                        errors.Add($"Path is not a .csproj file: {fullPath}");
                    }

                    continue;
                }

                if (Directory.Exists(fullPath))
                {
                    foreach (var file in Directory.EnumerateFiles(fullPath, "*.csproj", searchOption))
                    {
                        results.Add(Path.GetFullPath(file));
                    }

                    continue;
                }

                errors.Add($"Path not found: {fullPath}");
            }

            return results.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static bool IsCsproj(string path)
        {
            return string.Equals(Path.GetExtension(path), ".csproj", StringComparison.OrdinalIgnoreCase);
        }

        private sealed class ConsoleLog : ILog
        {
            private readonly bool verbose;

            public ConsoleLog(bool verbose)
            {
                this.verbose = verbose;
            }

            public void WriteLine(string message)
            {
                if (this.verbose)
                {
                    Console.WriteLine(message);
                    return;
                }

                if (message.StartsWith("Updating ", StringComparison.OrdinalIgnoreCase)
                    || message.StartsWith("Would update ", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine(message);
                }
            }
        }
    }
}
