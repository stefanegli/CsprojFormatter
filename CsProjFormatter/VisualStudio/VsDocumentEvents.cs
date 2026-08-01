namespace CsProjFormatter.VisualStudio
{
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;

    using System;

    /// <summary>
    /// Reports saves from Visual Studio's running document table, including CPS project files.
    /// Based on the Community.VisualStudio.Toolkit document-event implementation.
    /// </summary>
    public sealed class VsDocumentEvents : IVsRunningDocTableEvents
    {
        private RunningDocumentTable Documents { get; }

        public VsDocumentEvents()
        {
            this.Documents = new RunningDocumentTable();
            this.Documents.Advise(this);
        }

        public event EventHandler<VsDocument> Saved;

        int IVsRunningDocTableEvents.OnAfterFirstDocumentLock(uint docCookie, uint lockType, uint readLocksRemaining, uint editLocksRemaining)
        {
            return VSConstants.S_OK;
        }

        int IVsRunningDocTableEvents.OnBeforeLastDocumentUnlock(uint docCookie, uint lockType, uint readLocksRemaining, uint editLocksRemaining)
        {
            return VSConstants.S_OK;
        }

        int IVsRunningDocTableEvents.OnAfterSave(uint docCookie)
        {
            if (this.Saved != null)
            {
                var info = this.Documents.GetDocumentInfo(docCookie);
                var document = new VsDocument(this.Documents, docCookie, info.Moniker);
                this.Saved.Invoke(this, document);
            }

            return VSConstants.S_OK;
        }

        int IVsRunningDocTableEvents.OnAfterAttributeChange(uint docCookie, uint attributes)
        {
            return VSConstants.S_OK;
        }

        int IVsRunningDocTableEvents.OnBeforeDocumentWindowShow(uint docCookie, int firstShow, IVsWindowFrame frame)
        {
            return VSConstants.S_OK;
        }

        int IVsRunningDocTableEvents.OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame frame)
        {
            return VSConstants.S_OK;
        }
    }
}
