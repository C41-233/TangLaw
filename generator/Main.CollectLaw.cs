using Generator.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Text;
using static System.Collections.Specialized.BitVector32;

namespace Generator;

internal partial class Main
{

    private struct EntryTitle : IComparable<EntryTitle>
    {
        public int Number;
        public int Number2;
        public string Title;

        public string SeqNumber => Number2 == 0 ? $"{Number}" : $"{Number}.{Number2}";

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

            if (tokens[0].Contains('.'))
            {
                var numbers = tokens[0].Split(".", 2);
                return new EntryTitle
                {
                    Number = int.Parse(numbers[0]),
                    Number2 = int.Parse(numbers[1]),
                    Title = tokens[1]
                };
            }
            
            return new EntryTitle
            {
                Number = int.Parse(tokens[0]),
                Title = tokens[1]
            };
        }

        public int CompareTo(EntryTitle other)
        {
            var r1 = Comparer<int>.Default.Compare(this.Number, other.Number);
            if (r1 != 0)
            {
                return r1;
            }
            return Comparer<int>.Default.Compare(this.Number2, other.Number2);
        }
    }

    // 章
    private class LawSection1
    {
        public EntryTitle EntryTitle;
        public Document Content;
        public List<LawSection2> Children = new();

        public string FullTitle => $"第{Chinese.Parse(EntryTitle.Number)}章 {EntryTitle.Title}";
    }

    // 节
    private class LawSection2
    {
        public EntryTitle EntryTitle;
        public Document Content;
        public List<LawArticle> Children = new();

        public string FullTitle => $"第{Chinese.Parse(EntryTitle.Number)}节 {EntryTitle.Title}";
    }

    // 条
    private class LawArticle
    {
        public EntryTitle EntryTitle;
        public Document Content;
        public string FullTitle
        {
            get
            {
                if (EntryTitle.Number2 != 0)
                {
                    return $"第{Chinese.Parse(EntryTitle.Number)}条之{Chinese.Parse(EntryTitle.Number2)} {EntryTitle.Title}";
                }
                return $"第{Chinese.Parse(EntryTitle.Number)}条 {EntryTitle.Title}";
            }
        }
    }

    private readonly List<LawSection1> Section1List = new();

    // 正文
    private void CollectLaws()
    {
        var root = Path.Combine(SrcDir, "正文");
        foreach (var filename in Directory.GetDirectories(root))
        {
            Section1List.Add(CollectSection1(filename));
        }
        Section1List.Sort((e1, e2) => Comparer<EntryTitle>.Default.Compare(e1.EntryTitle, e2.EntryTitle));
    }

    private static LawSection1 CollectSection1(string path)
    {
        var filename = Path.GetFileNameWithoutExtension(path);
        var entryTitle = EntryTitle.Parse(filename);
        var section1 = new LawSection1
        {
            EntryTitle = entryTitle,
        };
        var content = Path.Combine(path, "-.xml");
        if (File.Exists(content))
        {
            section1.Content = new Document(content)
            {
                Title = section1.FullTitle,
                Output = $"{filename}.html",
            };
        }

        foreach (var sub in Directory.GetDirectories(path))
        {
            section1.Children.Add(CollectSection2(sub));
        }

        section1.Children.Sort((e1, e2) => Comparer<EntryTitle>.Default.Compare(e1.EntryTitle, e2.EntryTitle));

        return section1;
    }

    private static LawSection2 CollectSection2(string path)
    {
        var filename = Path.GetFileNameWithoutExtension(path);
        var entryTitle = EntryTitle.Parse(filename);
        var section2 = new LawSection2
        {
            EntryTitle = entryTitle,
        };
        var content = Path.Combine(path, "-.xml");
        if (File.Exists(content))
        {
            section2.Content = new Document(content)
            {
                Title = section2.FullTitle,
                Output = $"{filename}.html",
            };
        }

        foreach (var sub in Directory.GetFiles(path))
        {
            if (Path.GetFileNameWithoutExtension(sub) == "-")
            {
                continue;
            }
            section2.Children.Add(CollectArticle(sub));
        }

        section2.Children.Sort((e1, e2) => Comparer<EntryTitle>.Default.Compare(e1.EntryTitle, e2.EntryTitle));

        return section2;
    }

    private static LawArticle CollectArticle(string path)
    {
        var filename = Path.GetFileNameWithoutExtension(path);
        var entryTitle = EntryTitle.Parse(filename);
        var article = new LawArticle
        {
            EntryTitle = entryTitle,
        };
        article.Content = new Document(path)
        {
            Title = article.FullTitle,
            Output = $"{entryTitle.SeqNumber}.html",
        };
        return article;
    }
}
