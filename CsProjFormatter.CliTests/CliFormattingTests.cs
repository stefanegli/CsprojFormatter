// Copyright (c) 2026 by Stefan Egli.All rights reserved

namespace CsProjFormatter.CliTests
{
    using CsProjFormatterTests.TestFoundation;

    using NFluent;

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;

    using Xunit;

    public class CliFormattingTests
    {
        public static IEnumerable<object[]> FormattingCases =>
            FormattingCaseSource.Create(Path.Combine(AppContext.BaseDirectory, "_files"))
                .Select(testCase => new object[]
                {
                    testCase.RelativePath,
                    testCase.InputFile,
                    testCase.ExpectedFile,
                    testCase.CaseName,
                });

        [Theory]
        [MemberData(nameof(FormattingCases))]
        public async Task Cli_formats_existing_test_cases(string relativePath, string inputFile, string expectedFile, string caseName)
        {
            var tempRoot = Path.Combine(
                Path.GetTempPath(),
                "CsProjFormatter.CliTests",
                Guid.NewGuid().ToString("N", System.Globalization.CultureInfo.InvariantCulture));

            Directory.CreateDirectory(tempRoot);

            try
            {
                var stagedInputRoot = Path.Combine(tempRoot, "input");
                CopyDirectory(Path.Combine(AppContext.BaseDirectory, "_files", "input"), stagedInputRoot);

                var stagedFile = Path.Combine(stagedInputRoot, relativePath);
                var result = await RunCliAsync(stagedInputRoot, stagedFile);

                var failureContext = $"Case: {caseName}{Environment.NewLine}stdout:{Environment.NewLine}{result.StandardOutput}{Environment.NewLine}stderr:{Environment.NewLine}{result.StandardError}";
                Check.WithCustomMessage(failureContext).That(result.ExitCode).IsEqualTo(0);
                Check.WithCustomMessage(failureContext).That(result.StandardOutput).Contains(Path.GetFileName(relativePath));
                Check.WithCustomMessage(failureContext).That(File.ReadAllText(stagedFile)).Equals(File.ReadAllText(expectedFile));

                if (!string.Equals(File.ReadAllText(inputFile), File.ReadAllText(expectedFile), StringComparison.Ordinal))
                {
                    Check.WithCustomMessage(failureContext).That(result.StandardOutput).Contains("[updated]");
                }
            }
            finally
            {
                if (Directory.Exists(tempRoot))
                {
                    Directory.Delete(tempRoot, recursive: true);
                }
            }
        }

        private static async Task<CliRunResult> RunCliAsync(string workingDirectory, string targetFile)
        {
            var startInfo = new ProcessStartInfo("dotnet")
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                WorkingDirectory = workingDirectory,
            };

            startInfo.ArgumentList.Add(GetCliDllPath());
            startInfo.ArgumentList.Add("-v");
            startInfo.ArgumentList.Add(targetFile);

            using (var process = Process.Start(startInfo))
            {
                if (process is null)
                {
                    throw new InvalidOperationException("Failed to start the CLI process.");
                }

                var standardOutputTask = process.StandardOutput.ReadToEndAsync();
                var standardErrorTask = process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync().ConfigureAwait(false);
                return new CliRunResult(
                    process.ExitCode,
                    await standardOutputTask.ConfigureAwait(false),
                    await standardErrorTask.ConfigureAwait(false));
            }
        }

        private static string GetCliDllPath()
        {
            var configuration = typeof(CliFormattingTests).Assembly
                .GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration
                ?? "Debug";

            var repositoryRoot = FindRepositoryRoot();
            var cliDllPath = Path.Combine(
                repositoryRoot,
                "artifacts",
                "bin",
                "CsProjFormatter.Cli",
                configuration.ToLowerInvariant(),
                "csprojfmt.dll");

            if (!File.Exists(cliDllPath))
            {
                throw new FileNotFoundException($"CLI assembly not found: {cliDllPath}");
            }

            return cliDllPath;
        }

        private static string FindRepositoryRoot()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);

            while (current is not null)
            {
                if (File.Exists(Path.Combine(current.FullName, "CsProjFormatter.slnx")))
                {
                    return current.FullName;
                }

                current = current.Parent;
            }

            throw new DirectoryNotFoundException("Repository root could not be located.");
        }

        private static void CopyDirectory(string sourceRoot, string destinationRoot)
        {
            foreach (var directory in Directory.GetDirectories(sourceRoot, "*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(sourceRoot, directory);
                Directory.CreateDirectory(Path.Combine(destinationRoot, relativePath));
            }

            Directory.CreateDirectory(destinationRoot);
            foreach (var file in Directory.GetFiles(sourceRoot, "*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(sourceRoot, file);
                var destinationFile = Path.Combine(destinationRoot, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);
                File.Copy(file, destinationFile, overwrite: true);
            }
        }

        private sealed class CliRunResult
        {
            public CliRunResult(int exitCode, string standardOutput, string standardError)
            {
                this.ExitCode = exitCode;
                this.StandardOutput = standardOutput;
                this.StandardError = standardError;
            }

            public int ExitCode { get; }

            public string StandardOutput { get; }

            public string StandardError { get; }
        }
    }
}
