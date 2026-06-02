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

            // Format: X.Y-Z  or  X-Z  or  X.Y  or  X
            // X = article number, Y = sub-article number (optional), Z = paragraph number (optional)
            int paragraphNum = 0;
            string seqNumber;

            var dashIndex = text.LastIndexOf('-');
            if (dashIndex >= 0)
            {
                seqNumber = text[..dashIndex];
                paragraphNum = int.Parse(text[(dashIndex + 1)..]);
            }
            else
            {
                seqNumber = text;
            }

            if (Main.Instance.ArticleLawMap.TryGetValue(seqNumber, out var law) && law != null)
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
