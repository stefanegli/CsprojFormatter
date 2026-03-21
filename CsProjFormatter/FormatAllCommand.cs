// Copyright (c) 2026 by Stefan Egli.All rights reserved

namespace CsProjFormatter
{
    using EnvDTE;
    using EnvDTE80;

    using Microsoft.VisualStudio.Shell;

    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Design;
    using System.Linq;
    using System.Threading.Tasks;

    internal sealed class FormatAllCommand
    {
        private const string SolutionFolderProjectKind = "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}";

        public const int CommandId = 0x0100;

        public static readonly Guid CommandSet = new Guid("96f15caa-cbdc-4ddf-8f33-d44c16cb50dc");

        private readonly DTE2 dte;
        private readonly ILog log;

        private FormatAllCommand(OleMenuCommandService commandService, DTE2 dte, ILog log)
        {
            this.dte = dte ?? throw new ArgumentNullException(nameof(dte));
            this.log = log ?? throw new ArgumentNullException(nameof(log));

            var menuCommandId = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandId);
            commandService.AddCommand(menuItem);
        }

        public static async Task InitializeAsync(AsyncPackage package, DTE2 dte, ILog log)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService is object)
            {
                _ = new FormatAllCommand(commandService, dte, log);
            }
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!this.dte.Solution.IsOpen)
            {
                this.log.WriteLine("FormatAll: no open solution.");
                return;
            }

            var csprojPaths = this.GetCsProjPaths()
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (csprojPaths.Count == 0)
            {
                this.log.WriteLine("FormatAll: no .csproj projects found in the current solution.");
                return;
            }

            this.log.WriteLine($"FormatAll: processing {csprojPaths.Count} .csproj file(s).");

            var updatedCount = 0;
            var unchangedCount = 0;
            var skippedCount = 0;
            var inactiveCount = 0;
            var failedCount = 0;

            foreach (var path in csprojPaths)
            {
                try
                {
                    var formatter = new ConfigurableCsProjFormatter(this.log);
                    formatter.Run(path);

                    if (!formatter.IsActive)
                    {
                        inactiveCount++;
                        this.log.WriteLine($"FormatAll: inactive via .editorconfig, skipped '{path}'.");
                    }
                    else if (formatter.IsSkipped)
                    {
                        skippedCount++;
                        this.log.WriteLine($"FormatAll: skipped non-SDK-style project '{path}'.");
                    }
                    else if (formatter.IsFileChanged)
                    {
                        updatedCount++;
                        this.log.WriteLine($"FormatAll: updated '{path}'.");
                    }
                    else
                    {
                        unchangedCount++;
                        this.log.WriteLine($"FormatAll: already formatted '{path}'.");
                    }
                }
                catch (Exception ex)
                {
                    failedCount++;
                    this.log.WriteLine($"FormatAll: failed '{path}'. {ex.Message}");
                }
            }

            this.log.WriteLine($"FormatAll complete. Updated={updatedCount}, Unchanged={unchangedCount}, Skipped={skippedCount}, Inactive={inactiveCount}, Failed={failedCount}, Total={csprojPaths.Count}.");
        }

        private IEnumerable<string> GetCsProjPaths()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Project project in this.dte.Solution.Projects)
            {
                this.CollectProjectPaths(project, result);
            }

            return result;
        }

        private void CollectProjectPaths(Project project, ISet<string> paths)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (project == null)
            {
                return;
            }

            string projectKind;
            try
            {
                projectKind = project.Kind;
            }
            catch (Exception ex)
            {
                this.log.WriteLine($"FormatAll: cannot read project kind for '{this.GetProjectName(project)}'. {ex.Message}");
                return;
            }

            if (string.Equals(projectKind, SolutionFolderProjectKind, StringComparison.OrdinalIgnoreCase))
            {
                if (project.ProjectItems == null)
                {
                    return;
                }

                foreach (ProjectItem item in project.ProjectItems)
                {
                    this.CollectProjectPaths(item?.SubProject, paths);
                }

                return;
            }

            string fullName;
            try
            {
                fullName = project.FullName;
            }
            catch (Exception ex)
            {
                this.log.WriteLine($"FormatAll: cannot resolve project path for '{this.GetProjectName(project)}'. {ex.Message}");
                return;
            }

            if (fullName.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                paths.Add(fullName);
            }
        }

        private string GetProjectName(Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                return project?.Name ?? "<unknown>";
            }
            catch
            {
                return "<unknown>";
            }
        }
    }
}
