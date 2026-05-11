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
                entry.Children.Add(CollectAppendixInternal(child));
            }
            entry.Children.Sort((e1, e2) => Comparer<EntryTitle>.Default.Compare(e1.EntryTitle, e2.EntryTitle));
        }

        return entry;
    }
}
