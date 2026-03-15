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
            }
            catch (Exception ex)
            {
                log?.WriteLine("Failed to parse EditorConfig file:\n" + ex.ToString());
            }

            this.IsActive = isActive;

            bool IsEnabled(string setting) => "true" == setting;
        }

        public bool IsActive { get; }

        public bool SortEntries { get; }
    }
}
