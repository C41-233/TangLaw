namespace Generator.Models;

public class SrcDocument
{
    public required string RelativePath { get; init; }
    public required string Category { get; init; }
    public string? SubCategory { get; init; }
    public required string SrcText { get; init; }
    public required string LawText { get; init; }
    public required string FileName { get; init; }

    public string OutputPath => Path.ChangeExtension(RelativePath, ".html").Replace('\\', '/');
}
