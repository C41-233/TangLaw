using Generator;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: Generator <srcDir> <outputDir>");
    return 1;
}

var srcDir = args[0];
var outputDir = args[1];

try
{
    var generator = new Main(srcDir, outputDir);
    generator.Run();
}
catch (Exception ex) when (SourceException.IsSourceError(ex))
{
    // 文档本身的错误：只输出错误信息链，不输出调用堆栈
    for (var e = ex; e != null; e = e.InnerException)
    {
        Console.Error.WriteLine(e.Message);
    }
    return 1;
}
return 0;
