using Generator.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Generator;

internal partial class Main
{

    private struct EntryTitle
    {
        public int Number;
        public string Title;

        public static EntryTitle Parse(string name)
        {
            var tokens = name.Split('-', 2);
            if (tokens.Length < 2)
            {
                return new EntryTitle
                {
                    Title = tokens[0],
                };
            }
            return new EntryTitle
            {
                Number = int.Parse(tokens[0]),
                Title = tokens[1]
            };
        }
    }

    // 章
    private class LawSection1
    {
        public EntryTitle EntryTitle;
        public Document Content;

        public string FullTitle => $"第{Chinese.Parse(EntryTitle.Number)}章 {EntryTitle.Title}";
    }

    private readonly List<LawSection1> Section1List = new();

    // 正文
    private void CollectLaws()
    {
        var root = Path.Combine(SrcDir, "正文");
        foreach (var filename in Directory.GetDirectories(root))
        {
            CollectSection1(filename);
        }
        Section1List.Sort((e1, e2) => Comparer<int>.Default.Compare(e1.EntryTitle.Number, e2.EntryTitle.Number));
    }

    private void CollectSection1(string path)
    {
        var filename = Path.GetFileName(path);
        var entryTitle = EntryTitle.Parse(filename);
        var section1 = new LawSection1
        {
            EntryTitle = entryTitle,
        };
        Section1List.Add(section1);
        var content = Path.Combine(path, "-.xml");
        if (File.Exists(content))
        {
            section1.Content = new Document(content)
            {
                Title = section1.FullTitle,
                Output = $"{entryTitle.Number}.html",
            };
        }
    }

}
