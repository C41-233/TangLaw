using Generator.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

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

    // 序言
    private void ProcessPreamble()
    {
        var output = Path.Combine(DestDir, Preamble.Output);
        var writer = new Writer(output);
        writer.WriteLine(Preamble.GetBody());
        writer.Flush();
    }

}
