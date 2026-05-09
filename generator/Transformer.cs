using Generator.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

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
            TransformArticleLink();
            TransformText(doc.DocumentElement);
            TransformHeader();
            TransformLaw();
            TransformSrc();
            TransformWord();
            TransformContainer();
        }

        private void TransformArticleLink()
        {
            foreach (XmlElement node in doc.SelectNodes(".//article"))
            {
                var text = node.InnerText.Trim();
                var a = doc.CreateElement("a");
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

        private void TransformText(XmlElement node)
        {
            var toRemove = new List<XmlNode>();
            foreach (var child in node.ChildNodes)
            {
                if (child is XmlText textNode)
                {
                    var sections = textNode.InnerText.Trim().Split("\r\n\r\n");
                    foreach (var section in sections)
                    {
                        var p = doc.CreateElement("p");
                        p.InnerText = section.Trim();
                        textNode.ParentNode.InsertBefore(p, textNode);
                    }
                    toRemove.Add(textNode);
                }
            }
            foreach (var remove in toRemove)
            {
                Remove(remove);
            }
            toRemove.Clear();

            // 相邻的a需要合并
            foreach (XmlElement a in node.SelectNodes(".//a"))
            {
                var prev = a.PreviousSibling as XmlElement;
                var next = a.NextSibling as XmlElement;
                prev.InnerXml += a.OuterXml + next.InnerXml;
                toRemove.Add(a);
                toRemove.Add(next);
            }
            foreach (var remove in toRemove)
            {
                Remove(remove);
            }
            toRemove.Clear();
        }

        private void Remove(XmlNode node)
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
