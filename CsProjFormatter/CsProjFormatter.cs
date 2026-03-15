// Copyright (c) 2022 by Stefan Egli.All rights reserved

namespace CsProjFormatter
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml;
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
            var originalText = File.ReadAllText(resxPath);
            var document = XDocument.Load(resxPath);

            if (this.Settings.SortEntries)
            {
                if (IsProjectDocument(document))
                {
                    SortPropertyGroups(document);
                }
                else
                {
                    SortResxEntries(document);
                }
            }

            var formattedText = FormatDocument(document, this.Settings);
            if (!string.Equals(originalText, formattedText, StringComparison.Ordinal))
            {
                File.WriteAllText(resxPath, formattedText);
                this.Log.WriteLine($"Updating {resxPath}");
                return true;
            }

            var reason = "No modifications";
            this.Log.WriteLine($"Update was not required: {reason}.");
            return false;
        }

        private static string FormatDocument(XDocument document, ISettings settings)
        {
            var indentChars = ResolveIndentChars(settings);
            var newLineChars = ResolveNewLineChars(settings);
            var writerSettings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = indentChars,
                NewLineChars = newLineChars,
                NewLineHandling = NewLineHandling.Replace,
                OmitXmlDeclaration = document.Declaration is null,
            };

            using (var stringWriter = new StringWriter())
            using (var xmlWriter = XmlWriter.Create(stringWriter, writerSettings))
            {
                document.Save(xmlWriter);
                xmlWriter.Flush();
                return stringWriter.ToString();
            }
        }

        private static bool IsProjectDocument(XDocument document)
        {
            return document.Root?.Name.LocalName == "Project";
        }

        private static string ResolveIndentChars(ISettings settings)
        {
            if (string.Equals(settings.IndentStyle, "tab", StringComparison.OrdinalIgnoreCase))
            {
                return "\t";
            }

            var width = settings.TabWidth > 0 ? settings.TabWidth : 2;
            return new string(' ', width);
        }

        private static string ResolveNewLineChars(ISettings settings)
        {
            if (string.Equals(settings.EndOfLine, "lf", StringComparison.OrdinalIgnoreCase))
            {
                return "\n";
            }

            if (string.Equals(settings.EndOfLine, "cr", StringComparison.OrdinalIgnoreCase))
            {
                return "\r";
            }

            return "\r\n";
        }

        private static void SortPropertyGroups(XDocument document)
        {
            foreach (var propertyGroup in document.Root.Elements().Where(e => e.Name.LocalName == "PropertyGroup"))
            {
                var nodes = propertyGroup.Nodes().ToList();
                var groups = new List<ElementGroup>();
                var leadingNodes = new List<XNode>();

                foreach (var node in nodes)
                {
                    if (node is XElement element)
                    {
                        groups.Add(new ElementGroup(element, new List<XNode>(leadingNodes)));
                        leadingNodes.Clear();
                    }
                    else
                    {
                        leadingNodes.Add(node);
                    }
                }

                var trailingNodes = new List<XNode>(leadingNodes);
                if (groups.Count == 0)
                {
                    continue;
                }

                var sortedGroups = groups
                    .OrderBy(group => group.Element.Name.LocalName, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var newNodes = new List<XNode>();
                foreach (var group in sortedGroups)
                {
                    newNodes.AddRange(group.LeadingNodes);
                    newNodes.Add(group.Element);
                }

                newNodes.AddRange(trailingNodes);
                propertyGroup.ReplaceNodes(newNodes);
            }
        }

        private static void SortResxEntries(XDocument document)
        {
            if (document.Root is null)
            {
                return;
            }

            var toSave = new List<XNode>();
            var toSort = new List<XElement>();

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

            var sorted = toSort
                .OrderBy(e => e.Name.ToString(), StringComparer.OrdinalIgnoreCase)
                .ThenBy(e => (string)e.Attribute("name"), StringComparer.OrdinalIgnoreCase)
                .ToList();

            toSave.AddRange(sorted);
            document.Root.ReplaceNodes(toSave);
        }

        private sealed class ElementGroup
        {
            public ElementGroup(XElement element, List<XNode> leadingNodes)
            {
                this.Element = element;
                this.LeadingNodes = leadingNodes;
            }

            public XElement Element { get; }

            public List<XNode> LeadingNodes { get; }
        }
    }
}