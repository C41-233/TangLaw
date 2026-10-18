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
    private readonly string SourcePath;

    public Document(string path)
    {
        SourcePath = path;
        try
        {
            var content = File.ReadAllText(path);
            this.doc = new XmlDocument();
            doc.LoadXml($"<doc>{content}</doc>");

            Law = doc.SelectSingleNode("/doc/law")?.InnerText.Trim();
        }
        catch (Exception ex)
        {
            // 读取、解析阶段的错误都源于文档本身
            throw new SourceException($"处理文档「{SourcePath}」时发生错误", ex);
        }
    }

    public string GetBody()
    {
        try
        {
            var copy = (XmlDocument)doc.CloneNode(true);
            var transformer = new Transformer(copy);
            transformer.AddTitle(Title);
            transformer.Run();
            return copy.DocumentElement.InnerXml.ToString();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"处理文档「{SourcePath}」（{Title}）时发生错误", ex);
        }
    }

}
