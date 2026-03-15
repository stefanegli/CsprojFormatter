namespace CsProjFormatter
{
    public class ConfigurableCsProjFormatter
    {
        public ConfigurableCsProjFormatter(ILog log)
        {
            this.Log = log;
        }

        public bool IsFileChanged { get; private set; }

        private ILog Log { get; }

        /// <summary>
        /// Runs formatting if EditorConfig enables it for the target file.
        /// </summary>
        public void Run(string csprojPath)
        {
            var settings = new CsProjEditorConfigSettings(csprojPath, this.Log);
            if (!settings.IsActive)
            {
                return;
            }

            var formatter = new CsProjFormatter(settings, this.Log);
            this.IsFileChanged = formatter.Run(csprojPath);
        }
    }
}
