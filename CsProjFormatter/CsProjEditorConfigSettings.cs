using System;

namespace CsProjFormatter
{
    internal class CsProjEditorConfigSettings : ISettings
    {
        public CsProjEditorConfigSettings(string targetFile = "dummy.csproj", ILog log = null)
        {
            var isActive = false;
            try
            {
                var parser = new EditorConfig.Core.EditorConfigParser();
                var settings = parser.Parse(targetFile).Properties;
                if (settings.TryGetValue("csproj_formatter_sort_entries", out var sortEntries))
                {
                    isActive = true;
                    this.SortEntries = IsEnabled(sortEntries);
                }

                if (settings.TryGetValue("indent_style", out var indentStyle))
                {
                    isActive = true;
                    this.IndentStyle = indentStyle;
                }

                if (settings.TryGetValue("tab_width", out var tabWidth)
                    && int.TryParse(tabWidth, out var parsedTabWidth)
                    && parsedTabWidth > 0)
                {
                    isActive = true;
                    this.TabWidth = parsedTabWidth;
                }

                if (settings.TryGetValue("indent_size", out var indentSize)
                    && int.TryParse(indentSize, out var parsedIndentSize)
                    && parsedIndentSize > 0)
                {
                    isActive = true;
                    if (this.TabWidth == 0)
                    {
                        this.TabWidth = parsedIndentSize;
                    }
                }

                if (settings.TryGetValue("end_of_line", out var endOfLine))
                {
                    isActive = true;
                    this.EndOfLine = endOfLine;
                }
            }
            catch (Exception ex)
            {
                log?.WriteLine("Failed to parse EditorConfig file:\n" + ex.ToString());
            }

            this.IsActive = isActive;

            bool IsEnabled(string setting) => string.Equals(setting, "true", StringComparison.OrdinalIgnoreCase);
        }

        public bool IsActive { get; }

        public bool SortEntries { get; }

        public string IndentStyle { get; } = "space";

        public int TabWidth { get; } = 2;

        public string EndOfLine { get; } = "crlf";
    }
}
