using System;
using System.Collections.Generic;
using System.Text;
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
                    var lines = law.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
                    var paragraphs = new List<string>();
                    var sb = new StringBuilder();
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line))
                        {
                            if (sb.Length > 0) { paragraphs.Add(sb.ToString().TrimEnd()); sb.Clear(); }
                        }
                        else
                        {
                            if (sb.Length > 0) sb.AppendLine();
                            sb.Append(line.Trim());
                        }
                    }
                    if (sb.Length > 0) paragraphs.Add(sb.ToString().TrimEnd());
                    if (paragraphNum <= paragraphs.Count)
                    {
                        span.InnerText = $"「{paragraphs[paragraphNum - 1]}」";
                    }
                }
                else
                {
                    span.InnerText = $"「{law}」";
                }
            }

            Replace(node, span);
        }
    }

}
