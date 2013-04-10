
namespace Otter.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;
    using MarkdownSharp;
    using HtmlAgilityPack;

    public sealed class MarkdownConverter : ITextToHtmlConverter
    {
        private static readonly Dictionary<string, string[]> whitelist = CreateWhitelist();

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
            return list;
        }

        public string Convert(string plainText)
        {
            var markdown = new Markdown();
            string html = markdown.Transform(plainText);
            html = Purify(html);
            return html;
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
                string[] allowedAttributes = whitelist[node.Name];

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
            var childrenToRemove = node.ChildNodes.Where(n => n.NodeType == HtmlNodeType.Element && !whitelist.ContainsKey(n.Name)).ToArray();
            for (int i = 0; i < childrenToRemove.Length; i++)
            {
                node.RemoveChild(childrenToRemove[i], false);
            }

            foreach (var child in node.ChildNodes)
            {
                PurifyNode(child);
            }
        }
    }
}