using Generator;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: Generator <srcDir> <outputDir>");
    return 1;
}

var srcDir = args[0];
var outputDir = args[1];

var generator = new Main(srcDir, outputDir);
generator.Run();
return 0;
