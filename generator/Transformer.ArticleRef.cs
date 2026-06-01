using System;
using System.Xml;

namespace Generator;

internal partial class Transformer
{

    private static void TransformArticleRef(XmlElement root)
    {
        foreach (XmlElement node in root.SelectNodes(".//article-ref"))
        {
            var text = node.InnerText.Trim();
            var span = root.OwnerDocument.CreateElement("span");
            span.SetAttribute("class", "article-ref");

            int articleNum;
            int paragraphNum = 0;

            if (text.Contains("-"))
            {
                var tokens = text.Split("-");
                articleNum = int.Parse(tokens[0]);
                paragraphNum = int.Parse(tokens[1]);
            }
            else
            {
                articleNum = int.Parse(text);
            }

            if (Main.Instance.ArticleLawMap.TryGetValue(articleNum, out var law) && law != null)
            {
                if (paragraphNum > 0)
                {
                    var paragraphs = law.Split("\r\n\r\n", StringSplitOptions.RemoveEmptyEntries);
                    if (paragraphNum <= paragraphs.Length)
                    {
                        span.InnerText = paragraphs[paragraphNum - 1].Trim();
                    }
                }
                else
                {
                    span.InnerText = law;
                }
            }

            Replace(node, span);
        }
    }

}
