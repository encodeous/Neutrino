// using System.Text;
// using System.Text.Unicode;
// using Neutrino;
// using Neutrino.ContentSearch;
// using Neutrino.ContentSearch.Filters;
// using Neutrino.Utils;
//
// var fi = new FileInfo("testing.txt");
//
// var data = File.OpenRead("testing.txt");
// var br = new BufferedStream(data, 1 << 15);
//
// var start = DateTime.UtcNow;
//
// var searcher = new ContentSearcher();
//
// var byt = Encoding.UTF8.GetBytes("Hello, World!l@");
//
// var filter = new MatchContextBuilder()
//     .WithFilter(new AnyFilter())
//     .WithFilter(new EqualFilter(byt));
// var ctx = searcher.AddPattern("Hello World Matcher", filter);
//
// searcher.Build();
//
// int c;
//
// while ((c = br.ReadByte()) != -1)
// {
//     if(c == '\r' || c == '\n') continue;
//     searcher.AddByte((byte)c);
// }
//
// Console.WriteLine($"Match: {ctx.IsMatch}, elapsed: {DateTime.UtcNow - start}, collisions: {searcher.Collisions}");
//
// foreach (var v in ctx.Results)
// {
//     Console.WriteLine(v);
// }

// var searcher = new Searcher(new DirectoryInfo("C:\\"), new SearchOptions("**/*.txt", Concurrency: 5));
//
// var ct = new CancellationTokenSource();
//
// Console.CancelKeyPress += (sender, e) =>
// {
//     ct.Cancel();
//     e.Cancel = true;
// };
//
// int cnt = 0;
//
// var start = DateTime.UtcNow;
//
// await foreach(var file in searcher.SearchAsync(ct.Token)){
//     Console.WriteLine(file.FullFilePath);
//     cnt++;
// }
//
// Console.WriteLine($"Found: {cnt},  Time: {DateTime.UtcNow - start}");

using System.CommandLine;
using System.Text;
using ByteSizeLib;
using Neutrino.Cli;
using Neutrino.Cli.Data;
using Neutrino.ContentSearch;

var curDir = new DirectoryInfo(Environment.CurrentDirectory);

var root = new RootCommand("Neutrino is a lightning-speed file searcher.");

var outputFormatter = new Option<bool>("--json", "Formats the output as JSON.");
var path = new Option<string>("--path", "Specifies the directory to search in.");
path.SetDefaultValue(curDir.FullName);
path.AddValidator((v) =>
{
    var cp = v.GetValueOrDefault<string>();
    if (!Directory.Exists(cp))
    {
        v.ErrorMessage = $"The specified path \"{cp}\" does not exist.";
    }
});

var concurrency = new Option<int>("--conc", "Specifies the degree of concurrency to perform operations.");
concurrency.SetDefaultValue((Environment.ProcessorCount + 1) / 2);
concurrency.AddValidator((v) =>
{
    var val = v.GetValueOrDefault<int>();
    if (val < 1) v.ErrorMessage = "Cannot have a concurrency of less than 1.";
});
concurrency.AddAlias("-c");

var maxSize = new Option<string>("--max-size", $"Max file size for full-text-search operations. Valid sizes are in [KB, MB, GB, etc.]");
maxSize.SetDefaultValue("1MB");
maxSize.AddAlias("-s");
maxSize.AddValidator((v) =>
{
    var val = v.GetValueOrDefault<string>();
    var parsed = ByteSize.TryParse(val, out var bytes);
    if (!parsed)
    {
        v.ErrorMessage = $"The specified size \"{val}\" is not a valid size.";
    }

    if (bytes.Bytes >= long.MaxValue)
    {
        v.ErrorMessage = $"The specified size \"{val}\" exceeds the maximum representable file size supported by Neutrino. (Given: {bytes.Bytes} bytes, Maximum Allowed: {long.MaxValue} bytes)";
    }
});

root.Add(path);
root.Add(outputFormatter);
root.Add(concurrency);
root.Add(maxSize);

var globArg = new Argument<string>("file-name-pattern", "A glob pattern to match file names.");
globArg.SetDefaultValue("**/*");
globArg.Arity = ArgumentArity.ExactlyOne;

var searchArg = new Argument<string>("text-search-pattern", "Specifies a full-text-search pattern. See the documentation on the pattern format.");
searchArg.SetDefaultValue("");
searchArg.Arity = ArgumentArity.ExactlyOne;
searchArg.AddValidator((v) =>
{
    var val = v.GetValueOrDefault<string>();
    if (!PatternParser.TryParse(val, out _, Encoding.UTF8))
    {
        v.ErrorMessage = "Failed to parse the full-text-search pattern";
    }
});

root.Add(globArg);
root.Add(searchArg);

var cts = new CancellationTokenSource();

Console.CancelKeyPress += (e, c) =>
{
    cts.Cancel();
    c.Cancel = true;
};

root.SetHandler(async (string glob, bool isJson, int concur, string size, string search, string cPath) =>
{
    var opt = new SearchOptions(glob, isJson, concur, (long)ByteSize.Parse(size).Bytes, search, cPath);
    var searcher = new NeutrinoSearcher(opt);
    await searcher.SearchAsync(cts.Token);
}, globArg, outputFormatter, concurrency, maxSize, searchArg, path);


return root.Invoke(args);