// Copyright (c) 2022 by Stefan Egli.All rights reserved

namespace CsProjFormatter
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
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
            if (settings.IndentStyle == '\t')
            {
                return "\t";
            }

            var width = settings.TabWidth > 0 ? settings.TabWidth : 2;
            return new string(settings.IndentStyle, width);
        }

        private static string ResolveNewLineChars(ISettings settings)
        {
            return string.IsNullOrEmpty(settings.EndOfLine) ? "\r\n" : settings.EndOfLine;
        }

        private static List<ElementGroup> SortElementGroupsWithDependencies(List<ElementGroup> groups)
        {
            if (groups.Count <= 1)
            {
                return groups;
            }

            var comparer = StringComparer.OrdinalIgnoreCase;
            var nameToIndices = new Dictionary<string, List<int>>(comparer);
            for (var i = 0; i < groups.Count; i++)
            {
                var name = groups[i].Element.Name.LocalName;
                if (!nameToIndices.TryGetValue(name, out var indices))
                {
                    indices = new List<int>();
                    nameToIndices.Add(name, indices);
                }

                indices.Add(i);
            }

            var edges = new List<HashSet<int>>(groups.Count);
            var indegree = new int[groups.Count];
            for (var i = 0; i < groups.Count; i++)
            {
                edges.Add(new HashSet<int>());
            }

            var referenceRegex = new Regex(@"\$\(([^)]+)\)", RegexOptions.Compiled);
            for (var i = 0; i < groups.Count; i++)
            {
                var element = groups[i].Element;
                var text = element.Value;
                foreach (var attribute in element.Attributes())
                {
                    text += " " + attribute.Value;
                }

                foreach (Match match in referenceRegex.Matches(text))
                {
                    if (match.Groups.Count < 2)
                    {
                        continue;
                    }

                    var referenceName = match.Groups[1].Value;
                    if (string.IsNullOrWhiteSpace(referenceName))
                    {
                        continue;
                    }

                    if (!nameToIndices.TryGetValue(referenceName, out var indices))
                    {
                        continue;
                    }

                    foreach (var referencedIndex in indices)
                    {
                        if (referencedIndex == i)
                        {
                            continue;
                        }

                        if (edges[referencedIndex].Add(i))
                        {
                            indegree[i]++;
                        }
                    }
                }
            }

            var ready = new List<int>();
            for (var i = 0; i < indegree.Length; i++)
            {
                if (indegree[i] == 0)
                {
                    ready.Add(i);
                }
            }

            var result = new List<ElementGroup>(groups.Count);
            while (ready.Count > 0)
            {
                ready.Sort((left, right) =>
                {
                    var leftName = groups[left].Element.Name.LocalName;
                    var rightName = groups[right].Element.Name.LocalName;
                    var nameCompare = comparer.Compare(leftName, rightName);
                    return nameCompare != 0 ? nameCompare : left.CompareTo(right);
                });

                var next = ready[0];
                ready.RemoveAt(0);
                result.Add(groups[next]);

                foreach (var dependent in edges[next])
                {
                    indegree[dependent]--;
                    if (indegree[dependent] == 0)
                    {
                        ready.Add(dependent);
                    }
                }
            }

            if (result.Count == groups.Count)
            {
                return result;
            }

            var remaining = new List<int>();
            for (var i = 0; i < groups.Count; i++)
            {
                if (!result.Contains(groups[i]))
                {
                    remaining.Add(i);
                }
            }

            remaining.Sort((left, right) =>
            {
                var leftName = groups[left].Element.Name.LocalName;
                var rightName = groups[right].Element.Name.LocalName;
                var nameCompare = comparer.Compare(leftName, rightName);
                return nameCompare != 0 ? nameCompare : left.CompareTo(right);
            });

            foreach (var index in remaining)
            {
                result.Add(groups[index]);
            }

            return result;
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

                var sortedGroups = SortElementGroupsWithDependencies(groups);

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
