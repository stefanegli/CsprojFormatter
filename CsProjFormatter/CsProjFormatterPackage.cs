// Copyright (c) 2022 by Stefan Egli.All rights reserved

namespace CsProjFormatter
{
    using global::CsProjFormatter.Commands;
    using global::CsProjFormatter.VisualStudio;

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
        private static VsDocumentEvents documentEvents;

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var applicationObject = await this.GetServiceAsync(typeof(SDTE)) as EnvDTE80.DTE2;
            if (applicationObject is null)
            {
                throw new InvalidOperationException("Failed to get the Visual Studio automation service.");
            }

            await FormatAllCommand.InitializeAsync(this, applicationObject, Log.Current);

            // Keep the event source alive for the lifetime of the package.
            documentEvents = new VsDocumentEvents();
            documentEvents.Saved += this.OnDocumentSaved;

            Log.Current.WriteLine("CsProjFormatter initialized. Monitoring saved .csproj files.");
        }

        private void OnDocumentSaved(object sender, VsDocument document)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!document.Path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            Log.Current.WriteLine($"Save event received: {document.Path}");

            try
            {
                var formatter = new ConfigurableCsProjFormatter(Log.Current);
                formatter.Run(document.Path);

                if (!formatter.IsActive)
                {
                    Log.Current.WriteLine($"Save: inactive via .editorconfig, skipped '{document.Path}'.");
                }
                else if (formatter.IsSkipped)
                {
                    Log.Current.WriteLine($"Save: skipped non-SDK-style project '{document.Path}'.");
                }
                else if (formatter.IsFileChanged)
                {
                    Log.Current.WriteLine($"Save: updated '{document.Path}'.");
                }
                else
                {
                    Log.Current.WriteLine($"Save: already formatted '{document.Path}'.");
                }
            }
            catch (Exception ex)
            {
                Log.Current.WriteLine($"Save: failed '{document.Path}'. {ex}");
            }
        }
    }
}
