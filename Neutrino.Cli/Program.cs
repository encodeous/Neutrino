using System.Text;
using System.Text.Unicode;
using Neutrino;
using Neutrino.ContentSearch;

var data = File.OpenRead("testing.txt");
var bufI = new BufferedStream(data);

var searcher = new ContentSearcher();

var filter = new MatchContextBuilder()
    .WithFilter(ContentFilter.CreateEquals(Encoding.UTF8.GetBytes("Hello, World!")))
    .WithFilter(ContentFilter.CreateEquals(Encoding.UTF8.GetBytes("Hello, World!")))
    .WithFilter(ContentFilter.CreateEquals(Encoding.UTF8.GetBytes("Hello, World!")));

var ctx = searcher.AddPattern("Hello World Matcher", filter);

searcher.Build();

int c = 0;

while ((c = bufI.ReadByte()) != -1)
{
    if(c == '\r' || c == '\n') continue;
    searcher.AddByte((byte)c);
}

Console.WriteLine(ctx.IsMatch);

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