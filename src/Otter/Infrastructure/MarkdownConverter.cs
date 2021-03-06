﻿//-----------------------------------------------------------------------
// <copyright file="MarkdownConverter.cs" company="Dave Mateer">
// The MIT License (MIT)
//
// Copyright (c) 2014 Dave Mateer
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------
namespace Otter
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml;
    using CommonMark;
    using HtmlAgilityPack;

    public sealed class MarkdownConverter : ITextToHtmlConverter
    {
        private static readonly Dictionary<string, string[]> Whitelist = CreateWhitelist();

        public string Convert(string plainText, string articleUrlTitle)
        {
            // Render tables
            string html = Regex.Replace(plainText, @"^\s*\{\|.*?\|}\s*$", new MatchEvaluator(RenderTable), RegexOptions.Singleline | RegexOptions.Multiline);

            // Replace article path
            if (!string.IsNullOrEmpty(articleUrlTitle))
            {
                html = Regex.Replace(html, @"(!\[[^\]]*]\()_img/(\S+)", string.Format("$1%ARTICLE_IMAGES%/{0}/$2", articleUrlTitle));
            }

            html = CommonMarkConverter.Convert(html);
            html = Purify(html);
            return html;
        }

        private static Dictionary<string, string[]> CreateWhitelist()
        {
            var list = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            list.Add("a", new[] { "href", "title" });
            list.Add("b", null);
            list.Add("blockquote", null);
            list.Add("code", null);
            list.Add("em", null);
            list.Add("h1", null);
            list.Add("h2", null);
            list.Add("h3", null);
            list.Add("i", null);
            list.Add("li", null);
            list.Add("ol", null);
            list.Add("p", null);
            list.Add("pre", null);
            list.Add("sub", null);
            list.Add("sup", null);
            list.Add("strong", null);
            list.Add("ul", null);
            list.Add("br", null);
            list.Add("hr", null);

            list.Add("table", null);
            list.Add("thead", null);
            list.Add("tbody", null);
            list.Add("tr", null);
            list.Add("td", null);
            list.Add("th", null);

            list.Add("img", new string[] { "alt", "src", "title" });

            return list;
        }

        private static string Purify(string html)
        {
            var document = new HtmlDocument();
            document.LoadHtml(html);
            PurifyNode(document.DocumentNode);
            return document.DocumentNode.WriteTo().Trim();
        }

        private static void PurifyNode(HtmlNode node)
        {
            if (node.NodeType == HtmlNodeType.Element && node.HasAttributes)
            {
                string[] allowedAttributes = Whitelist[node.Name];

                if (allowedAttributes == null || allowedAttributes.Length == 0)
                {
                    node.Attributes.RemoveAll();
                }

                // Remove invalid attributes.
                for (int i = node.Attributes.Count() - 1; i >= 0; i--)
                {
                    if (!allowedAttributes.Contains(node.Attributes[i].Name))
                    {
                        node.Attributes.RemoveAt(i);
                    }
                }
            }

            // Purify all child nodes.
            var childrenToRemove = node.ChildNodes.Where(n => n.NodeType == HtmlNodeType.Element && !Whitelist.ContainsKey(n.Name)).ToArray();
            for (int i = 0; i < childrenToRemove.Length; i++)
            {
                node.RemoveChild(childrenToRemove[i], false);
            }

            foreach (var child in node.ChildNodes)
            {
                PurifyNode(child);
            }
        }

        private static string RenderTable(Match match)
        {
            var cellContent = new StringBuilder();

            int columnCountVerify = 0;
            int currentRowColumnCount = 0;

            var context = new Stack<string>();

            var table = new StringBuilder(Environment.NewLine);
            var settings = new XmlWriterSettings();
            settings.OmitXmlDeclaration = true;
            var writer = XmlWriter.Create(table, settings);

            var lines = Regex.Split(match.Value, "\r?\n");
            foreach (string line in lines)
            {
                string trimmed = line.TrimStart(null);

                if (trimmed.StartsWith("{|", StringComparison.OrdinalIgnoreCase))
                {
                    if (context.Count > 0)
                    {
                        return match.Value;
                    }

                    writer.WriteStartElement("table");
                    context.Push("table");
                }
                else if (trimmed.StartsWith("|}"))
                {
                    if (context.Peek() == "td")
                    {
                        WriteCellContent(writer, cellContent, context);
                        currentRowColumnCount++;
                    }

                    if (context.Count == 0 || context.Peek() != "tr")
                    {
                        return match.Value;
                    }

                    if (columnCountVerify > 0 && currentRowColumnCount != columnCountVerify)
                    {
                        return match.Value;
                    }

                    writer.WriteEndElement();  // tr
                    context.Pop();

                    if (context.Count == 0 || context.Peek() != "tbody")
                    {
                        return match.Value;
                    }

                    writer.WriteEndElement();  // tbody
                    context.Pop();

                    if (context.Count == 0 || context.Peek() != "table")
                    {
                        return match.Value;
                    }

                    writer.WriteEndElement();  // table
                    context.Pop();
                }
                else if (trimmed.StartsWith("|-"))
                {
                    if (context.Peek() == "td" || context.Peek() == "th")
                    {
                        WriteCellContent(writer, cellContent, context);
                        currentRowColumnCount++;
                    }

                    if (context.Peek() == "tr")
                    {
                        if (columnCountVerify > 0 && currentRowColumnCount != columnCountVerify)
                        {
                            return match.Value;
                        }

                        columnCountVerify = currentRowColumnCount;
                        currentRowColumnCount = 0;
                        writer.WriteEndElement();  // tr
                        context.Pop();
                    }

                    if (context.Peek() != "tbody")
                    {
                        if (context.Peek() == "thead")
                        {
                            writer.WriteEndElement();  // thead
                            context.Pop();
                        }

                        writer.WriteStartElement("tbody");
                        context.Push("tbody");
                    }

                    writer.WriteStartElement("tr");
                    context.Push("tr");
                }
                else if (trimmed.StartsWith("!"))
                {
                    if (context.Peek() == "table")
                    {
                        writer.WriteStartElement("thead");
                        context.Push("thead");

                        writer.WriteStartElement("tr");
                        context.Push("tr");
                    }
                    else if (context.Peek() == "td" || context.Peek() == "th")
                    {
                        WriteCellContent(writer, cellContent, context);
                        currentRowColumnCount++;
                    }

                    string content = trimmed.Substring(1);

                    string[] columns = content.Split(new string[] { "!!" }, StringSplitOptions.None);
                    if (columns.Length == 1)
                    {
                        writer.WriteStartElement("th");
                        context.Push("th");
                        cellContent.AppendLine(content);
                    }
                    else
                    {
                        foreach (var column in columns)
                        {
                            writer.WriteStartElement("th");
                            writer.WriteString(column.Trim());
                            writer.WriteEndElement();  // th
                            currentRowColumnCount++;
                        }
                    }
                }
                else if (trimmed.StartsWith("|"))
                {
                    if (context.Peek() == "table")
                    {
                        writer.WriteStartElement("tbody");
                        context.Push("tbody");

                        writer.WriteStartElement("tr");
                        context.Push("tr");
                    }
                    else if (context.Peek() == "td" || context.Peek() == "th")
                    {
                        WriteCellContent(writer, cellContent, context);
                        currentRowColumnCount++;
                    }

                    string content = trimmed.Substring(1);

                    string[] columns = content.Split(new string[] { "||" }, StringSplitOptions.None);
                    if (columns.Length == 1)
                    {
                        writer.WriteStartElement("td");
                        context.Push("td");
                        cellContent.AppendLine(content);
                    }
                    else
                    {
                        foreach (var column in columns)
                        {
                            writer.WriteStartElement("td");
                            writer.WriteString(column.Trim());
                            writer.WriteEndElement();  // td
                            currentRowColumnCount++;
                        }
                    }
                }
                else
                {
                    if (context.Count == 0)
                    {
                        // leading or trailing blank line; ignore
                        continue;
                    }

                    if (context.Count == 0 || (context.Peek() != "td" && context.Peek() != "th"))
                    {
                        return match.Value;
                    }

                    cellContent.AppendLine(trimmed);
                }
            }

            if (context.Count > 0)
            {
                return match.Value;
            }

            writer.Flush();
            return table.AppendLine().ToString();
        }

        private static void WriteCellContent(XmlWriter writer, StringBuilder content, Stack<string> context)
        {
            while (content.Length > 0 && char.IsWhiteSpace(content[content.Length - 1]))
            {
                content.Remove(content.Length - 1, 1);
            }

            string html = string.Empty;
            if (content.Length == 0)
            {
                html = "&nbsp;";
            }
            else
            {
                html = CommonMarkConverter.Convert(content.ToString()).TrimEnd(null);
            }

            writer.WriteRaw(html);
            content.Clear();
            writer.WriteEndElement();  // td or th
            context.Pop();
        }
    }
}