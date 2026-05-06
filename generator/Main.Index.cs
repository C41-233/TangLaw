using Generator.Utils;
using System.Text;
using static System.Collections.Specialized.BitVector32;

namespace Generator;

internal partial class Main
{

    // 主页
    private void OutputIndex()
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

            // 目录部分
            writer.WriteLine("<section>");
            {
                writer.H2("目录");
                writer.WriteLine("<nav>");
                {
                    writer.WriteLine("<ul>");
                    writer.WriteLine($"<li>{HTML.Href(Preamble.Title, Preamble.Output)}</li>");
                    foreach (var section1 in Section1List)
                    {
                        OutputIndexSection1(writer, section1);
                    }
                    writer.WriteLine("</ul>");
                }
                writer.WriteLine("</nav>");
            }
            writer.WriteLine("</section>");

            // 律文部分
            writer.WriteLine("<section class=\"index-section-text\">");
            {
                writer.H2("律文");
                writer.BeginDiv("index-text");
                {
                    // 序言
                    {
                        writer.Div("index-title-preamble", Preamble.Title);
                        writer.PP(Preamble.Law);
                    }

                    // 章
                    foreach (var section1 in Section1List)
                    {
                        writer.Div("index-title-section1", section1.FullTitle);
                        // 节
                        foreach (var section2 in section1.Children)
                        {
                            writer.Div("index-title-section2", section2.FullTitle);
                            foreach(var article in section2.Children)
                            {
                                writer.Div("index-title-article", article.FullTitle);
                                writer.PP(article.Content.Law);
                            }
                        }
                    }
                }
                writer.EndDiv();
            }
            writer.WriteLine("</section>");
        }
        writer.EndDiv();
        writer.Flush();
    }

    private void OutputIndexSection1(Writer writer, LawSection1 section1)
    {
        if (section1.Content == null)
        {
            writer.WriteLine($"<li>{section1.FullTitle}</li>");
        }
        else
        {
            writer.WriteLine($"<li>{HTML.Href(section1.FullTitle, section1.Content.Output)}</a></li>");
        }
        if (section1.Children.Count > 0)
        {
            writer.WriteLine($"<ul>");
            foreach (var sub in section1.Children)
            {
                OutputIndexSection2(writer, sub);
            }
            writer.WriteLine($"</ul>");
        }
    }

    private void OutputIndexSection2(Writer writer, LawSection2 section2)
    {
        if (section2.Content == null)
        {
            writer.WriteLine($"<li>{section2.FullTitle}</li>");
        }
        else
        {
            writer.WriteLine($"<li>{HTML.Href(section2.FullTitle, section2.Content.Output)}</a></li>");
        }
        if (section2.Children.Count > 0)
        {
            writer.WriteLine($"<ul>");
            foreach (var sub in section2.Children)
            {
                OutputIndexArticle(writer, sub);
            }
            writer.WriteLine($"</ul>");
        }
    }

    private void OutputIndexArticle(Writer writer, LawArticle article)
    {
        writer.WriteLine($"<li>{HTML.Href(article.FullTitle, article.Content.Output)}</a></li>");
    }
}
