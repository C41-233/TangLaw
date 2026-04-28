using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Generator;

internal class Document
{

    public string? Law { get; }

    public required string Title;    
    public required string Output;

    private readonly XmlDocument doc;

    public Document(string path)
    {
        var content = File.ReadAllText(path);
        this.doc = new XmlDocument();
        doc.LoadXml($"<doc>{content}</doc>");

        Law = doc.SelectSingleNode("/doc/law")?.InnerText;
    }

    public string GetBody()
    {
        var copy = (XmlDocument)doc.CloneNode(true);
        var transformer = new Transformer(copy);
        transformer.AddTitle(Title);
        transformer.Run();
        return copy.DocumentElement.InnerXml.ToString();
    }

}
