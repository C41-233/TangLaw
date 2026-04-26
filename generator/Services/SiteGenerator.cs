using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Generator.Models;

namespace Generator.Services;

public partial class SiteGenerator
{
    private readonly string _srcRoot;
    private readonly string _docsRoot;

    public SiteGenerator(string srcRoot, string docsRoot)
    {
        _srcRoot = srcRoot;
        _docsRoot = docsRoot;
    }

    public IReadOnlyList<SrcDocument> LoadDocuments()
    {
        if (!Directory.Exists(_srcRoot))
        {
            Console.WriteLine($"警告: src 目录不存在: {_srcRoot}");
            return [];
        }

        var docs = new List<SrcDocument>();
        var files = Directory.GetFiles(_srcRoot, "*.xml", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            var doc = ParseFile(file);
            if (doc != null) docs.Add(doc);
        }

        return docs.OrderBy(d => d.RelativePath).ToList();
    }

    private SrcDocument? ParseFile(string fullPath)
    {
        try
        {
            // XML has multiple root elements (<src> and <law>), wrap in <root>
            var content = File.ReadAllText(fullPath);
            var xml = XDocument.Parse($"<root>{content}</root>");
            var srcElement = xml.Root?.Element("src");
            var lawElement = xml.Root?.Element("law");

            if (srcElement == null) return null;

            var relativePath = Path.GetRelativePath(_srcRoot, fullPath);
            var parts = relativePath.Replace('\\', '/').Split('/');

            var category = parts.Length > 0 ? parts[0] : "";
            var subCategory = parts.Length > 2 ? parts[1] : null;

            return new SrcDocument
            {
                RelativePath = relativePath,
                Category = category,
                SubCategory = subCategory,
                SrcText = CleanText(srcElement.Value),
                LawText = CleanText(lawElement?.Value ?? ""),
                FileName = Path.GetFileNameWithoutExtension(fullPath)
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"解析失败: {fullPath} - {ex.Message}");
            return null;
        }
    }

    private static string CleanText(string text)
    {
        return Regex.Replace(text.Trim(), @"\s+", " ");
    }

    public void Generate(IReadOnlyList<SrcDocument> documents)
    {
        if (!Directory.Exists(_docsRoot))
            Directory.CreateDirectory(_docsRoot);

        GenerateIndex(documents);
        foreach (var doc in documents)
        {
            GeneratePage(doc, documents);
        }

        CopyAssets();
        Console.WriteLine($"生成完成: {documents.Count} 个页面");
    }

    private string BuildSidebar(IReadOnlyList<SrcDocument> documents, string? currentPath, string rootPrefix)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"""<nav class="sidebar"><h1>《唐律疏议》<br><small>的理解与适用</small></h1>""");

        var categories = documents.GroupBy(d => d.Category);
        foreach (var category in categories)
        {
            var categoryLabel = GetCategoryLabel(category.Key);
            sb.AppendLine($"<div class=\"category\"><h2>{categoryLabel}</h2><ul>");

            if (category.Key == "序言")
            {
                var doc = category.Single();
                var active = doc.OutputPath == currentPath ? " class=\"active\"" : "";
                sb.AppendLine($"<li><a{active} href=\"{rootPrefix}{doc.OutputPath}\">序言</a></li>");
            }
            else
            {
                foreach (var doc in category)
                {
                    var title = GetDocTitle(doc);
                    var active = doc.OutputPath == currentPath ? " class=\"active\"" : "";
                    sb.AppendLine($"<li><a{active} href=\"{rootPrefix}{doc.OutputPath}\">{title}</a></li>");
                }
            }

            sb.AppendLine("</ul></div>");
        }

        sb.AppendLine("</nav>");
        return sb.ToString();
    }

    private void GenerateIndex(IReadOnlyList<SrcDocument> documents)
    {
        var sidebar = BuildSidebar(documents, null, "");
        var html = $$"""
<!DOCTYPE html>
<html lang="zh-CN">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1.0">
<title>《唐律疏议》的理解与适用</title>
<link rel="stylesheet" href="style.css">
</head>
<body>
<div class="layout">
{{sidebar}}
<main class="content">
<h2>欢迎</h2>
<p>《唐律疏议》是中国现存最完整、最早的一部封建法典。请从左侧目录选择篇章阅读。</p>
</main>
</div>
</body>
</html>
""";

        File.WriteAllText(Path.Combine(_docsRoot, "index.html"), html);
    }

    private void GeneratePage(SrcDocument doc, IReadOnlyList<SrcDocument> documents)
    {
        var title = GetDocTitle(doc);
        var sidebar = BuildSidebar(documents, doc.OutputPath, "../");

        var html = $$"""
<!DOCTYPE html>
<html lang="zh-CN">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1.0">
<title>{{title}} - 《唐律疏议》</title>
<link rel="stylesheet" href="../style.css">
</head>
<body>
<div class="layout">
{{sidebar}}
<main class="content">
<article>
<h2>{{title}}</h2>
<section class="src-text">
<h3>原文</h3>
{{HtmlEncode(doc.SrcText)}}
</section>
{{(string.IsNullOrEmpty(doc.LawText) ? "" : $"""
<section class="law-text">
<h3>释义</h3>
{HtmlEncode(doc.LawText)}
</section>
""")}}
</article>
</main>
</div>
</body>
</html>
""";

        var outputDir = Path.Combine(_docsRoot, Path.GetDirectoryName(doc.OutputPath) ?? "");
        if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);

        File.WriteAllText(Path.Combine(_docsRoot, doc.OutputPath), html);
    }

    private static string GetCategoryLabel(string category)
    {
        return category switch
        {
            "序言" => "序言",
            "正文" => "正文",
            "附录" => "附录",
            "释义" => "释义",
            _ => category
        };
    }

    private static string GetDocTitle(SrcDocument doc)
    {
        if (doc.Category == "序言")
            return "序言";

        return doc.SubCategory != null
            ? $"{doc.SubCategory} 第{doc.FileName}条"
            : $"{GetCategoryLabel(doc.Category)} 第{doc.FileName}条";
    }

    private static string HtmlEncode(string text)
    {
        return string.Join("<br>",
            text.Split('\n')
                .Select(line => System.Net.WebUtility.HtmlEncode(line.Trim())));
    }

    private void CopyAssets()
    {
        var css = """
* { margin: 0; padding: 0; box-sizing: border-box; }

body {
    font-family: "Noto Serif CJK SC", "Source Han Serif SC", "SimSun", "STSong", serif;
    line-height: 1.8;
    color: #333;
    background: #f5f0e8;
}

.layout {
    display: flex;
    min-height: 100vh;
}

.sidebar {
    width: 280px;
    background: #2c3e50;
    color: #ecf0f1;
    padding: 1.5rem;
    position: fixed;
    top: 0;
    left: 0;
    height: 100vh;
    overflow-y: auto;
}

.sidebar h1 {
    font-size: 1.2rem;
    margin-bottom: 1.5rem;
    border-bottom: 1px solid #4a6274;
    padding-bottom: 0.8rem;
}

.sidebar h1 small {
    font-size: 0.9rem;
    font-weight: normal;
    color: #bdc3c7;
}

.sidebar h2 {
    font-size: 1rem;
    margin: 1rem 0 0.5rem;
    color: #e67e22;
}

.sidebar ul {
    list-style: none;
}

.sidebar li {
    margin: 0.3rem 0;
}

.sidebar a {
    color: #bdc3c7;
    text-decoration: none;
    font-size: 0.9rem;
    display: block;
    padding: 0.2rem 0.5rem;
    border-radius: 3px;
    transition: all 0.2s;
}

.sidebar a:hover {
    color: #fff;
    background: #34495e;
}

.sidebar a.active {
    color: #fff;
    background: #e67e22;
}

.content {
    margin-left: 280px;
    flex: 1;
    padding: 2rem 3rem;
    max-width: 900px;
}

.content h2 {
    font-size: 1.6rem;
    color: #2c3e50;
    border-bottom: 2px solid #e67e22;
    padding-bottom: 0.5rem;
    margin-bottom: 1.5rem;
}

.content h3 {
    font-size: 1.2rem;
    color: #2c3e50;
    margin: 1.5rem 0 0.8rem;
}

.src-text, .law-text {
    background: #fff;
    padding: 1.5rem;
    border-radius: 4px;
    box-shadow: 0 1px 3px rgba(0,0,0,0.1);
    margin-bottom: 1.5rem;
    text-indent: 2em;
}

.law-text {
    border-left: 3px solid #e67e22;
}

@media (max-width: 768px) {
    .layout { flex-direction: column; }
    .sidebar {
        width: 100%;
        height: auto;
        position: static;
    }
    .content { margin-left: 0; padding: 1.5rem; }
}
""";
        File.WriteAllText(Path.Combine(_docsRoot, "style.css"), css);
    }
}
