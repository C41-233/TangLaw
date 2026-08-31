using Generator.Utils;

namespace Generator;

internal partial class Main
{

    private class AppendixEntry
    {
        public required EntryTitle EntryTitle;
        public Document Content;
        public List<AppendixEntry> Children = new();
    }

    private AppendixEntry Appendix;

    public void CollectAppendixes()
    {
        var root = Path.Combine(SrcDir, "附录");
        Appendix = CollectAppendixInternal(root);
    }

    private static AppendixEntry CollectAppendixInternal(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        var entry = new AppendixEntry
        {
            EntryTitle = EntryTitle.Parse(name),
        };

        var title = entry.EntryTitle.Title;

        if (File.Exists(path))
        {
            entry.Content = new Document(path)
            {
                Title = title,
                Output = $"附录-{title}.html",
            };
        }
        if (Directory.Exists(path))
        {
            foreach (var child in Directory.EnumerateFileSystemEntries(path))
            {
                // -.xml 是该目录自身的内容标记，与正文约定一致
                if (Path.GetFileNameWithoutExtension(child) == "-" && File.Exists(child))
                {
                    entry.Content = new Document(child)
                    {
                        Title = title,
                        Output = $"附录-{title}.html",
                    };
                    continue;
                }
                entry.Children.Add(CollectAppendixInternal(child));
            }
            entry.Children.Sort((e1, e2) => Comparer<EntryTitle>.Default.Compare(e1.EntryTitle, e2.EntryTitle));
        }

        return entry;
    }

    // 为每个一级附录分组生成目录页：一页列全该组完整目录
    private void OutputAppendixDirectories()
    {
        foreach (var appendix in Appendix.Children)
        {
            if (appendix.Children.Count == 0)
            {
                continue;
            }
            try
            {
                var writer = new Writer(Path.Combine(DestDir, $"附录-{appendix.EntryTitle.Title}.html"), appendix.EntryTitle.Title);
                writer.BeginDiv("container");
                writer.WriteLine("<header>");
                writer.WriteLine($"<h1>{appendix.EntryTitle.Title}</h1>");
                writer.WriteLine("</header>");
                writer.WriteLine("<nav>");
                writer.WriteLine("<ul>");
                foreach (var child in appendix.Children)
                {
                    OutputAppendixTree(writer, child);
                }
                writer.WriteLine("</ul>");
                writer.WriteLine("</nav>");
                writer.EndDiv();
                writer.Flush();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"输出附录目录「{appendix.EntryTitle.Title}」时发生错误", ex);
            }
        }
    }

    // 目录页递归渲染：有内容页的节点为链接，无内容节点仅作分组标题
    private void OutputAppendixTree(Writer writer, AppendixEntry entry)
    {
        if (entry.Content != null)
        {
            writer.WriteLine($"<li>{HTML.Href(entry.EntryTitle.Title, entry.Content.Output)}</li>");
        }
        else
        {
            writer.WriteLine($"<li>{entry.EntryTitle.Title}</li>");
        }
        if (entry.Children.Count > 0)
        {
            writer.WriteLine("<ul>");
            foreach (var child in entry.Children)
            {
                OutputAppendixTree(writer, child);
            }
            writer.WriteLine("</ul>");
        }
    }
}
