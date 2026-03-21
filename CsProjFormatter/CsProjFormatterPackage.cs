// Copyright (c) 2022 by Stefan Egli.All rights reserved

namespace CsProjFormatter
{
    using EnvDTE;

    using global::CsProjFormatter.Commands;

    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;

    using System;
    using System.Runtime.InteropServices;
    using System.Threading;

    using Task = System.Threading.Tasks.Task;

    [Guid(PackageGuids.guidCsProjFormatterPackageString)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class CsProjFormatterPackage : AsyncPackage
    {
        private static EnvDTE80.DTE2 applicationObject;
        private static DocumentEvents documentEvents;
        private static Events events;
        private static ILog Log { get; } = new Log();

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // avoid garbage collection
            applicationObject = await this.GetServiceAsync(typeof(SDTE)) as EnvDTE80.DTE2;
            if (applicationObject is object)
            {
                await FormatAllCommand.InitializeAsync(this, applicationObject, Log);
                events = applicationObject.Events;
                documentEvents = events.DocumentEvents;
                documentEvents.DocumentSaved += this.OnDocumentSaved;
            }
        }

        private void OnDocumentSaved(Document document)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (document.Kind.ToUpperInvariant() == "{8E7B96A8-E33D-11D0-A6D5-00C04FB67F6A}"
                && document.FullName.ToUpperInvariant().EndsWith(".CSPROJ"))
            {
                Log.WriteLine("Save event for xml document received.");
                var formatter = new ConfigurableCsProjFormatter(Log);
                formatter.Run(document.FullName);
            }
        }
    }
}