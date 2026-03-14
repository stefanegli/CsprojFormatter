// Copyright (c) 2022 by Stefan Egli.All rights reserved

namespace CsProjFormatter
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;

    public class CsProjFormatter
    {
        public CsProjFormatter(ISettings settings, ILog log)
        {
            this.Log = log;
            this.Settings = settings;
        }

        private ILog Log { get; }
        private ISettings Settings { get; }

        public bool Run(String resxPath)
        {
            var result = false;
            var toSave = new List<XNode>();
            var toSort = new List<XElement>();
            var document = XDocument.Load(resxPath);

            foreach (var node in document.Root.Nodes())
            {
                if (node is XElement element && (element.Name.LocalName == "data" || element.Name.LocalName == "metadata"))
                {
                    toSort.Add(element);
                }
                else
                {
                    toSave.Add(node);
                }
            }

            var sorted = this.Settings.SortEntries
                ? toSort.OrderBy(e => e.Attribute("name").Value).OrderBy(e => e.Name.ToString()).ToList()
                : toSort;

            var requiresSorting = this.Settings.SortEntries && !toSort.SequenceEqual(sorted);
            if (requiresSorting)
            {
                toSave.AddRange(sorted);
                document.Root.ReplaceNodes(toSave);
                this.Log.WriteLine($"Updating {resxPath}");
                document.Save(resxPath);
                result = true;
            }
            else
            {
                var reason = "No modifications";
                this.Log.WriteLine($"Update was not required: {reason}.");
            }

            return result;
        }
    }
}