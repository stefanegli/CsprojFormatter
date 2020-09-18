namespace CsProjFormatterTests
{
    using NFluent;
    using CsProjFormatter;
    using CsProjFormatterTests.TestFoundation;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;
    using Xunit;

    public class FormattingTests
    {
        [Theory]
        [ClassData(typeof(CsProjTestData))]
        public void Files_are_processed_correctly(ISettings settings, string message, string fileName, string expectedHash)
        {
            // Arrange
            var tempFileName = $"_files\\{Guid.NewGuid()}.resx";

            var formatter = new CsProjFormatter(settings, new FakeLog());
            var file = $"_files\\{fileName}";
            File.Copy(file, tempFileName, true);

            // Act
            formatter.Run(tempFileName);

            // Assert
            Check.WithCustomMessage(message + $" Result File: {tempFileName}").That(Sha256(tempFileName)).Equals(expectedHash);
        }

        private string Sha256(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = new SHA256Managed())
            {
                var result = new StringBuilder();
                byte[] hash = sha.ComputeHash(stream);
                foreach (byte hashByte in hash)
                {
                    result.Append(hashByte.ToString("X2"));
                }

                return result.ToString();
            }
        }

        internal class CsProjTestData : TheoryDataBase<ISettings, string, string, string>
        {
            public override IEnumerable<(ISettings, string, string, string)> Create()
            {
                var @default = new FakeSettings
                {
                    SortEntries = true,
                    RemoveDocumentationComment = true
                };

                yield return (@default, "...", "xx.csproj", "..");
            }

            private class FakeSettings : ISettings
            {
                public bool FixResxWriter => throw new NotImplementedException();
                public ReloadMode ReloadFile => throw new NotImplementedException();
                public bool RemoveDocumentationComment { get; set; }
                public bool SortEntries { get; set; }
            }
        }
    }
}