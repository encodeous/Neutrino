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
    private SemaphoreSlim _threadReturns;

    public NeutrinoSearcher(SearchOptions options)
    {
        _options = options;
        _fullTextSearchRequests = Channel.CreateBounded<SearchResult>(options.Concurrency);
    }

    public async Task SearchAsync(CancellationToken ct)
    {
        var availThreads = _options.Concurrency;
        _searchThreads = availThreads;
        _threadReturns = new SemaphoreSlim(_options.Concurrency);
        if (_options.MatchPattern != "")
        {
            _searchThreads = Math.Max(1, availThreads / 3);
            _matchThreads = Math.Max(1, availThreads - _searchThreads);
            for (int i = 0; i < _matchThreads; i++)
            {
                Task.Run(() =>
                {
                    return FullTextSearch(ct);
                }, ct);
            }
        }

        _root = new DirectoryInfo(_options.Path);
        _searcher = new FileSearcher(_root,
            new SearcherParameters(_options.Glob, Concurrency: _searchThreads));
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
            StartProcessing(ct);
            await foreach (var foundResult in _channelResults.Reader.ReadAllAsync(ct))
            {
                PrintResult(foundResult);
            }
        }
        else
        {
            StartProcessing(ct);
            await JsonSerializer.SerializeAsync(Console.OpenStandardOutput(), _channelResults.Reader.ReadAllAsync(ct), cancellationToken: ct);
        }
    }

    private void PrintResult(FoundResult foundResult)
    {
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
    private Channel<SearchResult> _fullTextSearchRequests;

    private async Task FullTextSearch(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var val = await _fullTextSearchRequests.Reader.ReadAsync(ct);
                var fi = new FileInfo(val.FullFilePath);
                if(fi.Length > _options.SizeBytes) continue;
                FileStream fs;
                try
                {
                    fs = File.OpenRead(val.FullFilePath);
                }
                catch
                {
                    // ignored
                    continue;
                }
                var buf = new byte[8192];
                var builder = new MatchContextBuilder()
                    .WithFilterString(_options.MatchPattern);
                var fts = new ContentSearcher();
                var ctx = fts.AddPattern(builder);
                fts.Build();
                int len;
                while ((len = await fs.ReadAsync(buf, ct)) != 0)
                {
                    for (int i = 0; i < len && !ct.IsCancellationRequested; i++)
                    {
                        fts.AddByte(buf[i]);
                    }
                }
                if (ctx.IsMatch)
                {
                    await _channelResults.Writer.WriteAsync(new MatchedResult(val.RelFilePath, ctx.Results.ToArray()), ct);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Unhandled exception: {e.Message}{e.StackTrace}");
        }
        finally
        {
            _threadReturns.Release();
        }
    }

    private void StartProcessing(CancellationToken ct)
    {
        Task.Run(async () =>
        {
            try
            {
                foreach (var res in _searcher.Search(ct))
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

                if (_options.MatchPattern != "")
                {
                    await _threadReturns.WaitAsync();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unhandled exception: {e.Message}{e.StackTrace}");
            }
            finally
            {
                _channelResults?.Writer.TryComplete();
                _fullTextSearchRequests?.Writer.TryComplete();
            }
        }, ct);
    }
}