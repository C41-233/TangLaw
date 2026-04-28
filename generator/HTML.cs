using System;
using System.Collections.Generic;
using System.Text;

namespace Generator
{
    internal static class HTML
    {

        public static string Href(string value, string href)
        {
            return $"<a href=\"{href}\">{value}</a>";
        }

    }
}
