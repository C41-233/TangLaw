using Generator.Utils;
using System.Text;

namespace Generator;

internal partial class Main
{

    // 主页
    private void ProcessIndex()
    {
        var output = Path.Combine(DestDir, "index.html");
        var writer = new Writer(output);
        writer.BeginDiv("container");
        {
            writer.WriteLine("<header>");
            {
                writer.WriteLine("<h1>《唐律》的理解与适用</h1>");
            }
            writer.WriteLine("</header>");

            writer.WriteLine("<section>");
            {
                writer.H2("目录");
                writer.WriteLine("<nav>");
                {
                    writer.WriteLine("<ul>");
                    writer.WriteLine($"<li>{HTML.Href(Preamble.Title, Preamble.Output)}</li>");
                    foreach (var section1 in Section1List)
                    {
                        if (section1.Content == null)
                        {
                            writer.WriteLine($"<li>{section1.FullTitle}</li>");
                        }
                        else
                        {
                            writer.WriteLine($"<li>{HTML.Href(section1.FullTitle, section1.Content.Output)}</a></li>");
                        }
                    }
                    writer.WriteLine("</ul>");
                }
                writer.WriteLine("</nav>");
            }
            writer.WriteLine("</section>");

            writer.WriteLine("<section class=\"index-section-text\">");
            {
                writer.H2("律文");
                writer.BeginDiv("index-text");
                {
                    writer.BeginDiv("index-text-entry");
                    writer.Div("index-title-preamble", Preamble.Title);
                    writer.PP(Preamble.Law);
                    writer.EndDiv();
                }
                writer.EndDiv();
            }
            writer.WriteLine("</section>");
        }
        writer.EndDiv();
        writer.Flush();
    }

}
