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
            Check.That(properties.Keys).Contains("csproj_formatter_sort_item_types");

            var sortEntries = properties["csproj_formatter_sort_entries"];
            Check.That(sortEntries.GetProperty("values").EnumerateArray().Select(value => value.GetBoolean())).Contains(true);
            Check.That(sortEntries.GetProperty("values").EnumerateArray().Select(value => value.GetBoolean())).Contains(false);
            Check.That(sortEntries.GetProperty("defaultValue").EnumerateArray().Single().GetBoolean()).IsTrue();

            var emptyLinesBetweenGroups = properties["csproj_formatter_empty_lines_between_groups"];
            Check.That(emptyLinesBetweenGroups.GetProperty("values").EnumerateArray().Select(value => value.GetInt32())).Contains(0);
            Check.That(emptyLinesBetweenGroups.GetProperty("defaultValue").EnumerateArray().Single().GetInt32()).IsEqualTo(1);
            Check.That(emptyLinesBetweenGroups.GetProperty("description").GetString()).Contains("non-negative integer");

            var sortItemTypes = properties["csproj_formatter_sort_item_types"];
            var sortItemTypeValues = sortItemTypes.GetProperty("values").EnumerateArray().Select(value => value.GetString());
            Check.That(sortItemTypeValues).Contains("*");
            Check.That(sortItemTypeValues).Contains("<item_type>");
            Check.That(sortItemTypeValues).Contains("PackageReference");
            Check.That(sortItemTypes.GetProperty("multiple").GetBoolean()).IsTrue();
            Check.That(sortItemTypes.GetProperty("description").GetString()).Contains("replacement");
            Check.That(sortItemTypes.GetProperty("description").GetString()).Contains("unlisted types retain");
        }

        [Fact]
        public void Visual_studio_integration_uses_unique_package_and_output_pane_ids()
        {
            const string resxFormatterPackageId = "40d1f52e-e828-4cca-8279-df4ccd348f09";
            const string resxFormatterOutputPaneId = "4DDD4974-C22A-4D9A-B148-3594680AAC76";
            const string packageId = "a02f620d-a31a-46a3-b2f5-7a0e214830f8";

            var generatedPackageIds = File.ReadAllText(GetProjectFilePath(Path.Combine("Commands", "CsProjFormatter.cs")));
            var commandTable = File.ReadAllText(GetProjectFilePath(Path.Combine("Commands", "CsProjFormatter.vsct")));
            var log = File.ReadAllText(GetProjectFilePath("Log.cs"));

            Check.That(generatedPackageIds).Contains(packageId);
            Check.That(commandTable).Contains(packageId);
            Check.That(generatedPackageIds).Not.Contains(resxFormatterPackageId);
            Check.That(commandTable).Not.Contains(resxFormatterPackageId);
            Check.That(log).Not.Contains(resxFormatterOutputPaneId);
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
