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
            TransformText(doc.DocumentElement);
            TransformHeader();
            TransformLaw();
            TransformSrc();
            TransformContainer();
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
            foreach (XmlElement node in doc.SelectNodes("//law"))
            {
                TransformText(node);
                var div = doc.CreateElement("div");
                div.SetAttribute("class", "law");
                Replace(node, div);
            }
        }
        
        private void TransformSrc()
        {
            // src要挪到最后
            foreach (XmlElement node in doc.SelectNodes("//src"))
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
        }

        private void Remove(XmlNode node)
        {
            node.ParentNode.RemoveChild(node);
        }

        private static void Replace(XmlElement from, XmlElement to)
        {
            to.InnerXml = from.InnerXml;
            from.ParentNode.ReplaceChild(to, from);
        }

    }

}
