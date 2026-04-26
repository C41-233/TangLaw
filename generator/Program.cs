using Generator.Services;

string FindProjectRoot()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir != null)
    {
        var srcPath = Path.Combine(dir.FullName, "src");
        var genPath = Path.Combine(dir.FullName, "generator");
        if (Directory.Exists(srcPath) && Directory.Exists(genPath))
            return dir.FullName;
        dir = dir.Parent;
    }
    throw new InvalidOperationException("未找到项目根目录（缺少 src 或 generator 目录）");
}

var srcRoot = args.Length > 0 ? Path.GetFullPath(args[0]) : Path.Combine(FindProjectRoot(), "src");
var docsRoot = args.Length > 1 ? Path.GetFullPath(args[1]) : Path.Combine(FindProjectRoot(), "docs");

Console.WriteLine($"src目录: {srcRoot}");
Console.WriteLine($"docs目录: {docsRoot}");

var generator = new SiteGenerator(srcRoot, docsRoot);
var documents = generator.LoadDocuments();
generator.Generate(documents);
