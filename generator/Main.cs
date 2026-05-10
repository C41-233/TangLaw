using Generator.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using static System.Collections.Specialized.BitVector32;

namespace Generator;

internal partial class Main
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
        CollectLaws();
        CollectWords();

        OutputIndex();
        OutputPreamble();
        OutputLaws();
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

    // 序言
    private void OutputPreamble()
    {
        var output = Path.Combine(DestDir, Preamble.Output);
        var writer = new Writer(output, Preamble.Title);
        writer.WriteLine(Preamble.GetBody());
        writer.Flush();
    }

    private void OutputLaws()
    {
        foreach (var section1 in Section1List)
        {
            OutputSection1(section1);
        }
    }

    private void OutputSection1(LawSection1 section1)
    {
        if (section1.Content != null)
        {
            var output = Path.Combine(DestDir, section1.Content.Output);
            var writer = new Writer(output, section1.Content.Title);
            writer.WriteLine(section1.Content.GetBody());
            writer.Flush();
        }
        foreach (var section2 in section1.Children)
        {
            OutputSection2(section2);
        }
    }

    private void OutputSection2(LawSection2 section2)
    {
        if (section2.Content != null)
        {
            var output = Path.Combine(DestDir, section2.Content.Output);
            var writer = new Writer(output, section2.Content.Title);
            writer.WriteLine(section2.Content.GetBody());
            writer.Flush();
        }
        foreach (var article in section2.Children)
        {
            OutputArticle(article);
        }
    }

    private void OutputArticle(LawArticle article)
    {
        var output = Path.Combine(DestDir, article.Content.Output);
        var writer = new Writer(output, article.Content.Title);
        writer.WriteLine(article.Content.GetBody());
        writer.Flush();
    }
}
