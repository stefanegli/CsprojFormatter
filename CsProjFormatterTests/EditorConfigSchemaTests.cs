namespace CsProjFormatterTests
{
    using NFluent;

    using System;
    using System.IO;
    using System.Linq;
    using System.Text.Json;

    using Xunit;

    public class EditorConfigSchemaTests
    {
        [Fact]
        public void Schema_declares_supported_formatter_properties()
        {
            using var schema = JsonDocument.Parse(File.ReadAllText(GetProjectFilePath("CsProjFormatter.editorconfig-schema.json")));
            var properties = schema.RootElement
                .GetProperty("properties")
                .EnumerateArray()
                .ToDictionary(property => property.GetProperty("name").GetString() ?? throw new InvalidOperationException("Schema property name missing."));

            Check.That(properties.Keys).Contains("csproj_formatter_sort_entries");
            Check.That(properties.Keys).Contains("csproj_formatter_empty_lines_between_groups");

            var sortEntries = properties["csproj_formatter_sort_entries"];
            Check.That(sortEntries.GetProperty("values").EnumerateArray().Select(value => value.GetBoolean())).Contains(true);
            Check.That(sortEntries.GetProperty("values").EnumerateArray().Select(value => value.GetBoolean())).Contains(false);
            Check.That(sortEntries.GetProperty("defaultValue").EnumerateArray().Single().GetBoolean()).IsTrue();

            var emptyLinesBetweenGroups = properties["csproj_formatter_empty_lines_between_groups"];
            Check.That(emptyLinesBetweenGroups.GetProperty("defaultValue").EnumerateArray().Single().GetInt32()).IsEqualTo(1);
            Check.That(emptyLinesBetweenGroups.GetProperty("description").GetString()).Contains("non-negative integer");
        }

        [Fact]
        public void Pkgdef_registers_editorconfig_schema()
        {
            var pkgdef = File.ReadAllText(GetProjectFilePath("CsProjFormatter.EditorConfigSchema.pkgdef"));

            Check.That(pkgdef).Contains(@"[$RootKey$\Languages\Language Services\EditorConfig\Schemas\CsProjFormatter]");
            Check.That(pkgdef).Contains(@"""schema""=""$PackageFolder$\CsProjFormatter.editorconfig-schema.json""");
            Check.That(pkgdef).Contains(@"""moniker""=""KnownMonikers.Settings""");
        }

        private static string GetProjectFilePath(string fileName)
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "CsProjFormatter.slnx")))
            {
                directory = directory.Parent;
            }

            if (directory is null)
            {
                throw new InvalidOperationException("Could not locate the repository root.");
            }

            return Path.Combine(directory.FullName, "CsProjFormatter", fileName);
        }
    }
}
