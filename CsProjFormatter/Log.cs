// Copyright (c) 2026 by Stefan Egli.All rights reserved

namespace CsProjFormatter
{
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.VisualStudio.Threading;

    using System;

    public class Log : ILog
    {
        private static IVsOutputWindowPane outputPane;

        private Log()
        {
        }

        public static ILog Current { get; } = new Log();

        private static IVsOutputWindowPane OutputPane
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                if (outputPane == null)
                {
                    outputPane = CreateOutputPane();
                }

                return outputPane;
            }
        }

        public void WriteLine(string message)
        {
            ThreadHelper.JoinableTaskFactory.StartOnIdle(() =>
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                WriteLineInternal(message);
            }).Task.FileAndForget("CsProjFormatter/Log");
        }

        private static IVsOutputWindowPane CreateOutputPane()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var outWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
            if (outWindow is null)
            {
                throw new InvalidOperationException("Failed to get the Visual Studio output window service.");
            }

            var guid = Guid.Parse("{c6a80368-57ef-4d70-b042-9290086e8dfa}");
            outWindow.CreatePane(ref guid, Vsix.Name, 1, 1);
            outWindow.GetPane(ref guid, out var generalPane);
            return generalPane;
        }

        private static void WriteLineInternal(string message)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var line = $"[{DateTime.Now.ToLongTimeString()}] {message}{Environment.NewLine}";
            OutputPane?.OutputString(line);
        }
    }
}
