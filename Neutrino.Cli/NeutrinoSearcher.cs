using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Channels;
using Neutrino.Cli.Data;
using Neutrino.ContentSearch;
using Neutrino.Searcher;
using Spectre.Console;
using SearchOptions = Neutrino.Cli.Data.SearchOptions;

namespace Neutrino.Cli;

public class NeutrinoSearcher
{
    private SearchOptions _options;
    private DirectoryInfo _root;
    private bool _advancedOutput;
    private int _searchThreads, _matchThreads;
    private FileSearcher _searcher;

    public NeutrinoSearcher(SearchOptions options)
    {
        _options = options;
    }

    public async Task SearchAsync(CancellationToken ct)
    {
        var availThreads = _options.Concurrency;
        _searchThreads = availThreads;
        if (_options.MatchPattern != String.Empty)
        {
            _searchThreads = Math.Max(1, availThreads / 3);
            _matchThreads = Math.Min(_searchThreads * 2, availThreads);
            for (int i = 0; i < _matchThreads; i++)
            {
                Task.Run(() =>
                {
                    return FullTextSearch(ct);
                }, ct);
            }
        }

        _root = new DirectoryInfo(Environment.CurrentDirectory);
        _searcher = new FileSearcher(_root,
            new SearcherParameters(_options.Glob, Concurrency: _searchThreads));
        StartProcessing(ct);
        if (!_options.IsJsonOutput)
        {
            if (AnsiConsole.Console.Profile.Capabilities.Ansi
                && AnsiConsole.Console.Profile.Capabilities.Interactive)
            {
                _advancedOutput = true;
                AnsiConsole.Write(
                    new FigletText("Neutrino")
                        .LeftAligned());
                AnsiConsole.Write(new Rule("Search Options").LeftAligned());
                AnsiConsole.WriteLine(_options.ToString());
                AnsiConsole.Write(new Rule());
            }
            await foreach (var foundResult in _channelResults.Reader.ReadAllAsync(ct))
            {
                PrintResult(foundResult);
            }
        }
        else
        {
            await JsonSerializer.SerializeAsync(Console.OpenStandardOutput(), _channelResults.Reader.ReadAllAsync(ct), cancellationToken: ct);
        }
    }

    private void PrintResult(FoundResult foundResult)
    {
        AnsiConsole.Write(_channelResults.Reader.Count + " " + _searcher.RequestQueue.Reader.Count + " ");
        string fileFmt = $"{foundResult.FilePath}";
        if (foundResult is MatchedResult m)
        {
            AnsiConsole.WriteLine($"{fileFmt} : Matches @ {string.Join(" - ", m.Matches.Select(x => { return $"[{x.MatchBegin}, {x.MatchEnd}]"; }))}");
        }
        else
        {
            AnsiConsole.WriteLine(fileFmt);
        }
    }

    private Channel<FoundResult> _channelResults = Channel.CreateUnbounded<FoundResult>();
    private Channel<SearchResult> _fullTextSearchRequests = Channel.CreateBounded<SearchResult>(FileSearcher.ChannelBound);

    private async Task FullTextSearch(CancellationToken ct)
    {
        while (!_fullTextSearchRequests.Reader.Completion.IsCompleted && !ct.IsCancellationRequested)
        {
            var val = await _fullTextSearchRequests.Reader.ReadAsync(ct);
            var fs = File.OpenRead(val.FullFilePath);
            var bsr = new BufferedStream(fs);
            var builder = new MatchContextBuilder()
                .WithFilterString(_options.MatchPattern);
            var fts = new ContentSearcher();
            var ctx = fts.AddPattern(builder);
            fts.Build();

            int data;
            while ((data = bsr.ReadByte()) != -1 && !ct.IsCancellationRequested)
            {
                fts.AddByte((byte)data);
            }

            if (ctx.IsMatch)
            {
                await _channelResults.Writer.WriteAsync(new MatchedResult(val.RelFilePath, ctx.Results.ToArray()), ct);
            }
        }
        _channelResults.Writer.TryComplete();
    }

    private void StartProcessing(CancellationToken ct)
    {
        Task.Run(async () =>
        {
            await foreach (var res in _searcher.SearchAsync(ct))
            {
                if (_options.MatchPattern == "")
                {
                    await _channelResults.Writer.WriteAsync(new FoundResult(res.RelFilePath), ct);
                }
                else
                {
                    await _fullTextSearchRequests.Writer.WriteAsync(res, ct);
                }
            }

            _channelResults.Writer.TryComplete();
            _fullTextSearchRequests.Writer.TryComplete();
        }, ct);
    }
}