using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Generator
{

    internal class Transformer
    {

        private readonly XmlDocument doc;

        public Transformer(XmlDocument doc)
        {
            this.doc = doc;
        }

        public void AddTitle(string value)
        {
            var title = doc.CreateElement("h1");
            title.InnerText = value;
            doc.DocumentElement.PrependChild(title);
        }

        public void Run()
        {
            TransformLaw();
            TransformSrc();
        }

        private void TransformLaw()
        {
            foreach (XmlElement node in doc.SelectNodes("//law"))
            {
                var div = doc.CreateElement("div");
                Replace(node, div);
            }
        }

        private void TransformSrc()
        {
            foreach (XmlElement node in doc.SelectNodes("//src"))
            {
                var div = doc.CreateElement("div");
                Replace(node, div);
            }
        }

        private static void Replace(XmlElement from, XmlElement to)
        {
            to.InnerXml = from.InnerXml;
            from.ParentNode.ReplaceChild(to, from);
        }

    }

}
