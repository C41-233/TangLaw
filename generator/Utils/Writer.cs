using System;
using System.Collections.Generic;
using System.Text;

namespace Generator.Utils;

internal class Writer
{

    private readonly StringBuilder sb = new ();
    private readonly string path;

    public Writer(string path, string title)
    {
        this.path = path;
        sb.AppendLine("<html>");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"UTF-8\">");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine("<link rel=\"stylesheet\" href=\"../styles/main.css\">");
        sb.AppendLine($"<title>{title}</title>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
    }

    public void Flush()
    {
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        File.WriteAllText(path, sb.ToString());
    }

    public void WriteLine(string value)
    {
        sb.AppendLine(value);
    }

    internal void WriteLine(object body)
    {
        throw new NotImplementedException();
    }
}
