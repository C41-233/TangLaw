using System;
using System.Collections.Generic;
using System.Text;

namespace Generator;

internal partial class Main
{

    public Dictionary<string, string> Words = new Dictionary<string, string>();

    private void CollectWords()
    {
        var root = Path.Combine(SrcDir, "释义");
        foreach (var file in Directory.GetFiles(root, "*.xml", SearchOption.AllDirectories))
        {
            var word = Path.GetFileNameWithoutExtension(file);
            if (Words.ContainsKey(word))
            {
                throw new SourceException($"词条「{word}」重复定义：{file}");
            }
            var content = File.ReadAllText(file);
            Words.Add(word, content);
        }
    }

}
