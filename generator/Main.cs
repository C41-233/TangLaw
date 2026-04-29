using Generator.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Generator;

internal class Main
{

    private readonly string SrcDir;
    private readonly string DestDir;

    public Main(string srcDir, string destDir)
    {
        SrcDir = srcDir;
        DestDir = destDir;
    }

    private Document Preamble;

    public void Run()
    {
        if (Directory.Exists(DestDir))
        {
            Directory.Delete(DestDir, true);
        }
        Directory.CreateDirectory(DestDir);

        CollectDocuments();

        ProcessIndex();
        ProcessPreamble();
    }

    private void CollectDocuments()
    {
        {
            var input = Path.Combine(SrcDir, "序言.xml");
            Preamble = new Document(input)
            {
                Output = "序言.html",
                Title = "序言",
            };
        }
    }

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

    // 序言
    private void ProcessPreamble()
    {
        var output = Path.Combine(DestDir, Preamble.Output);
        var writer = new Writer(output);
        writer.WriteLine(Preamble.GetBody());
        writer.Flush();
    }

}
