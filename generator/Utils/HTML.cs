using System;
using System.Collections.Generic;
using System.Text;

namespace Generator.Utils
{
    internal static class HTML
    {

        public static string Href(string value, string href)
        {
            return $"<a href=\"{href}\">{value}</a>";
        }

        public static void BeginDiv(this Writer writer, string style)
        {
            writer.WriteLine($"<div class=\"{style}\">");
        }

        public static void EndDiv(this Writer writer)
        {
            writer.WriteLine("</div>");
        }

        public static void Div(this Writer writer, string style, string content)
        {
            writer.WriteLine($"<div class=\"{style}\">{content}</div>");
        }

        public static void H2(this Writer writer, string content)
        {
            writer.WriteLine($"<h2>{content}</h2>");
        }

        public static void PP(this Writer writer, string content)
        {
            if (content == null)
            {
                return;
            }
            var tokens = content.Trim().Split("\r\n\r\n");
            foreach (var token in tokens)
            {
                writer.WriteLine($"<p>{token.Trim()}</p>");
            }
        }

    }
}
