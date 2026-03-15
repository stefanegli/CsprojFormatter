// Copyright (c) 2022 by Stefan Egli.All rights reserved

namespace CsProjFormatterTests
{
    using CsProjFormatter;

    using CsProjFormatterTests.TestFoundation;

    using NFluent;

    using System;
    using System.Collections.Generic;
    using System.IO;

    using Xunit;

    public class FormattingTests
    {
        [Theory]
        [ClassData(typeof(CsProjTestData))]
        public void Files_are_processed_correctly(string inputFile, string expectedFile, string caseName)
        {
            // Arrange
            var formatter = new ConfigurableCsProjFormatter(new FakeLog());

            // Act
            formatter.Run(inputFile);

            // Assert
            Check.WithCustomMessage($"Case: {caseName} Input: {inputFile} Expected: {expectedFile}")
                .That(File.ReadAllText(inputFile))
                .Equals(File.ReadAllText(expectedFile));
        }

        internal class CsProjTestData : TheoryDataBase<string, string, string>
        {
            public override IEnumerable<(string, string, string)> Create()
            {
                var outputRoot = Path.Combine(AppContext.BaseDirectory, "_files");
                var inputRoot = Path.Combine(outputRoot, "input");
                var expectedRoot = Path.Combine(outputRoot, "expected");

                if (!Directory.Exists(inputRoot))
                {
                    throw new InvalidOperationException($"Input folder not found: {inputRoot}");
                }

                foreach (var inputFile in Directory.GetFiles(inputRoot, "*.csproj", SearchOption.AllDirectories))
                {
                    var relativePath = GetRelativePath(inputRoot, inputFile);
                    var expectedFile = Path.Combine(expectedRoot, relativePath);

                    if (!File.Exists(expectedFile))
                    {
                        throw new InvalidOperationException($"Expected file not found: {expectedFile}");
                    }

                    var caseName = relativePath.Replace(Path.DirectorySeparatorChar, '/');
                    yield return (inputFile, expectedFile, caseName);
                }
            }

            private static string AppendDirectorySeparator(string path)
            {
                if (path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                {
                    return path;
                }

                return path + Path.DirectorySeparatorChar;
            }

            private static string GetRelativePath(string basePath, string fullPath)
            {
                var baseUri = new Uri(AppendDirectorySeparator(basePath), UriKind.Absolute);
                var fullUri = new Uri(fullPath, UriKind.Absolute);
                var relativeUri = baseUri.MakeRelativeUri(fullUri);
                return Uri.UnescapeDataString(relativeUri.ToString())
                    .Replace('/', Path.DirectorySeparatorChar);
            }

            private class FakeSettings : ISettings
            {
                public bool SortEntries { get; set; }
            }
        }
    }
}