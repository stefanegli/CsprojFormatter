namespace CsProjFormatter.VisualStudio
{
    using Microsoft.VisualStudio.Shell;

    public sealed class VsDocument
    {
        public VsDocument(RunningDocumentTable documents, uint cookie, string path)
        {
            this.Documents = documents;
            this.Cookie = cookie;
            this.Path = path;
        }

        public uint Cookie { get; }

        public string Path { get; }

        private RunningDocumentTable Documents { get; }
    }
}
