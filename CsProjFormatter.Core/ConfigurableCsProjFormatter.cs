// Copyright (c) 2026 by Stefan Egli.All rights reserved

namespace CsProjFormatter
{
    public class ConfigurableCsProjFormatter
    {
        public ConfigurableCsProjFormatter(ILog log)
        {
            this.Log = log;
        }

        public bool IsActive { get; private set; }

        public bool IsFileChanged { get; private set; }

        public bool IsSkipped { get; private set; }

        private ILog Log { get; }

        /// <summary>
        /// Runs formatting if EditorConfig enables it for the target file.
        /// </summary>
        public void Run(string csprojPath)
        {
            this.Run(csprojPath, true);
        }

        public void Run(string csprojPath, bool writeChanges)
        {
            this.IsFileChanged = false;
            this.IsSkipped = false;
            var settings = new CsProjEditorConfigSettings(csprojPath, this.Log);
            this.IsActive = settings.IsActive;
            if (!settings.IsActive)
            {
                return;
            }

            var formatter = new CsProjFormatter(settings, this.Log);
            var result = formatter.RunWithResult(csprojPath, writeChanges);
            this.IsFileChanged = result == FormatterRunResult.Updated;
            this.IsSkipped = result == FormatterRunResult.SkippedNonSdkStyle;
        }
    }
}
