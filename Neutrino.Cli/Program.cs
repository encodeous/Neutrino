using System.Text;
using Neutrino;
using Neutrino.TextSearch;

var data = File.ReadAllBytes("testing.txt");
var hasher = new Hasher(313, 1_000_000_007);
var key = new SearchKey(Encoding.UTF8.GetBytes("dabaam\r\ngood morning :)"), hasher);
var search = new RabinKarp(key.Key.Length, hasher);

int idx = 0;

foreach(byte b in data)
{
    search.AddByte(b);
    key.Increment();
    if (search.Matches(key))
    {
        Console.WriteLine("Found match at {0}", idx);
    }
    hasher.Increment();
    idx++;
}


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