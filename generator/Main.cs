using Generator.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using static System.Collections.Specialized.BitVector32;

namespace Generator;

internal partial class Main
{

    public static Main Instance { get; private set; }

    private readonly string SrcDir;
    private readonly string DestDir;

    public Main(string srcDir, string destDir)
    {
        Instance = this;
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
        CollectAppendixes();

        OutputIndex();
        OutputPreamble();
        OutputLaws();
        OutputAppendixes();
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
        try
        {
            var output = Path.Combine(DestDir, Preamble.Output);
            var writer = new Writer(output, Preamble.Title);
            writer.WriteLine(Preamble.GetBody());
            writer.Flush();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"输出「{Preamble.Title}」时发生错误", ex);
        }
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
            try
            {
                var output = Path.Combine(DestDir, section1.Content.Output);
                var writer = new Writer(output, section1.Content.Title);
                writer.WriteLine(section1.Content.GetBody());
                writer.Flush();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"输出篇「{section1.Content.Title}」时发生错误", ex);
            }
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
            try
            {
                var output = Path.Combine(DestDir, section2.Content.Output);
                var writer = new Writer(output, section2.Content.Title);
                writer.WriteLine(section2.Content.GetBody());
                writer.Flush();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"输出章「{section2.Content.Title}」时发生错误", ex);
            }
        }
        foreach (var article in section2.Children)
        {
            OutputArticle(article);
        }
    }

    private void OutputArticle(LawArticle article)
    {
        try
        {
            var output = Path.Combine(DestDir, article.Content.Output);
            var writer = new Writer(output, article.Content.Title);
            writer.WriteLine(article.Content.GetBody());
            writer.Flush();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"输出条「{article.Content.Title}」时发生错误", ex);
        }
    }

    private void OutputAppendixes()
    {
        OutputAppendix(Appendix);
        void OutputAppendix(AppendixEntry entry)
        {
            if (entry.Content != null)
            {
                try
                {
                    var output = Path.Combine(DestDir, entry.Content.Output);
                    var writer = new Writer(output, entry.Content.Title);
                    writer.WriteLine(entry.Content.GetBody());
                    writer.Flush();
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"输出附录「{entry.Content.Title}」时发生错误", ex);
                }
            }
            else
            {
                foreach(var child in entry.Children)
                {
                    OutputAppendix(child);
                }
            }
        }
    }

}
