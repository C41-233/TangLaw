using Generator.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using static System.Collections.Specialized.BitVector32;

namespace Generator
{

    internal class Transformer
    {

        private readonly XmlDocument doc;

        private string title;

        public Transformer(XmlDocument doc)
        {
            this.doc = doc;
        }

        public void AddTitle(string value)
        {
            title = value;
        }

        public void Run()
        {
            TransformNormal(doc.DocumentElement);
            TransformHeader();
            TransformLaw();
            TransformSrc();
            TransformWord();
            TransformContainer();
        }

        private static void TransformNormal(XmlElement root)
        {
            TransformArticleLink(root);
            TransformText(root);
        }

        private static void TransformArticleLink(XmlElement root)
        {
            foreach (XmlElement node in root.SelectNodes(".//article"))
            {
                var text = node.InnerText.Trim();
                var a = root.OwnerDocument.CreateElement("a");
                a.SetAttribute("href", $"{text}.html");
                a.SetAttribute("class", "link-article");
                ReplaceTag(node, a);

                if (text.Contains("."))
                {
                    var tokens = text.Split(".");
                    var number = int.Parse(tokens[0]);
                    var number2 = int.Parse(tokens[1]);
                    a.InnerText = $"第{Chinese.Parse(number)}条之{Chinese.Parse(number2)}";
                }
                else
                {
                    var number = int.Parse(text);
                    a.InnerText = $"第{Chinese.Parse(number)}条";
                }
            }
        }

        private void TransformHeader()
        {
            var header = doc.CreateElement("header");

            var h1 = doc.CreateElement("h1");
            h1.InnerText = title;

            header.AppendChild(h1);
            doc.DocumentElement.PrependChild(header);
        }

        private void TransformLaw()
        {
            foreach (XmlElement node in doc.SelectNodes(".//law"))
            {
                TransformText(node);
                var div = doc.CreateElement("div");
                div.SetAttribute("class", "law");
                ReplaceTag(node, div);
            }
        }
        
        private void TransformSrc()
        {
            // src要挪到最后
            foreach (XmlElement node in doc.SelectNodes(".//src"))
            {
                var parent = node.ParentNode;
                TransformText(node);
                var div = doc.CreateElement("div");
                div.SetAttribute("class", "src");
                div.InnerXml = node.InnerXml;
                Remove(node);
                parent.AppendChild(div);
            }
        }

        private void TransformWord()
        {
            foreach (XmlElement node in doc.SelectNodes(".//word"))
            {
                var name = node.GetAttribute("value");

                var word = doc.CreateElement("div");
                word.SetAttribute("class", "word");

                var title = doc.CreateElement("div");
                title.SetAttribute("class", "world-title");
                title.InnerText = name;
                word.AppendChild(title);

                var content = doc.CreateElement("div");
                content.SetAttribute("class", "word-content");

                content.InnerXml = Main.Words[name];
                TransformNormal(content);

                word.AppendChild(content);

                Replace(node, word);
            }
        }

        private void TransformContainer()
        {
            var div = doc.CreateElement("div");
            div.SetAttribute("class", "container");
            foreach (XmlNode node in doc.DocumentElement.ChildNodes)
            {
                div.AppendChild(node.CloneNode(true));
            }
            doc.DocumentElement.RemoveAll();
            doc.DocumentElement.AppendChild(div);
        }

        private static void TransformText(XmlElement node)
        {
            var splits = new List<(string, bool)>();
            var current = new StringBuilder();
            foreach (var child in node.ChildNodes)
            {
                if (child is XmlText text)
                {
                    var lines = text.InnerText.Split("\r\n");
                    for (int i = 0; i < lines.Length; i++)
                    {
                        string? line = lines[i];
                        if (line.Trim().Length == 0)
                        {
                            if (current.Length > 0)
                            {
                                splits.Add((current.ToString(), true));
                                current.Clear();
                            }
                            continue;
                        }
                        current.Append(line.Trim());
                        if (i < lines.Length - 1)
                        {
                            current.AppendLine();
                        }
                    }
                }
                else if (child is XmlElement element)
                {
                    if (element.Name == "a")
                    {
                        current.Append(element.OuterXml);
                    }
                    else
                    {
                        if (current.Length > 0)
                        {
                            splits.Add((current.ToString(), true));
                            current.Clear();
                        }
                        splits.Add((element.OuterXml, false));
                    }
                }
            }
            if (current.Length > 0)
            {
                splits.Add((current.ToString(), true));
            }
            current.Clear();

            node.InnerXml = "";
            foreach (var kv in splits)
            {
                if (kv.Item2)
                {
                    current.Append($"<p>{kv.Item1}</p>");
                }
                else
                {
                    current.Append(kv.Item1);
                }
            }
            node.InnerXml = current.ToString();
        }

        private static void Remove(XmlNode node)
        {
            node.ParentNode.RemoveChild(node);
        }

        private static void ReplaceTag(XmlElement from, XmlElement to)
        {
            to.InnerXml = from.InnerXml;
            from.ParentNode.ReplaceChild(to, from);
        }

        private static void Replace(XmlElement from, XmlElement to)
        {
            from.ParentNode.ReplaceChild(to, from);
        }

    }

}
