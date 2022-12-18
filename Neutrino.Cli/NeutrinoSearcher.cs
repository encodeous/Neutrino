using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Channels;
using ByteSizeLib;
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
    }

    public async Task SearchAsync(CancellationToken ct)
    {
        var availThreads = _options.Concurrency;
        _searchThreads = availThreads;
        _threadReturns = new SemaphoreSlim(_options.Concurrency);
        _fullTextSearchRequests = Channel.CreateBounded<SearchResult>(_options.Concurrency);
        _channelResults = Channel.CreateUnbounded<FoundResult>();
        if (_options.MatchPattern != "")
        {
            _searchThreads = Math.Max(1, availThreads / 3);
            _matchThreads = Math.Max(1, availThreads - _searchThreads);
            for (int i = 0; i < _matchThreads; i++)
            {
                Task.Run(() => FullTextSearch(ct), ct);
            }
        }

        _root = new DirectoryInfo(_options.Path);
        _searcher = new FileSearcher(_root,
            new SearcherParameters(_options.Glob, Concurrency: _searchThreads));
        var isAnsi = AnsiConsole.Console.Profile.Capabilities.Ansi
                     && AnsiConsole.Console.Profile.Capabilities.Interactive;
        if (!_options.IsJsonOutput)
        {
            if (isAnsi)
            {
                _advancedOutput = true;
                AnsiConsole.Write(
                    new FigletText("Neutrino")
                        .LeftAligned());
                AnsiConsole.Write(new Text("Accelerated File Searcher\n", new Style(decoration: Decoration.Italic)));
                AnsiConsole.Write(new Rule("Search Options").LeftAligned());
                AnsiConsole.WriteLine(_options.ToString());
                AnsiConsole.Write(new Rule());
            }

            var start = Stopwatch.GetTimestamp();
            try
            {
                StartProcessing(ct);
                await foreach (var foundResult in _channelResults.Reader.ReadAllAsync(ct))
                {
                    PrintResult(foundResult);
                }
            }
            finally
            {
                if (isAnsi)
                {
                    AnsiConsole.Write(new Rule("Search Results").LeftAligned());
                    if (_options.MatchPattern == "")
                    {
                        AnsiConsole.WriteLine($"{_searcher.Statistics.ObjectsGlobMatched:N0} match(es) out of {_searcher.Statistics.ObjectsDiscovered:N0} object(s) in {Stopwatch.GetElapsedTime(start)}.");
                    }
                    else
                    {
                        AnsiConsole.WriteLine($"{_searcher.Statistics.ObjectsContentMatched:N0} match(es) out of {_searcher.Statistics.ObjectsDiscovered:N0} object(s), with {ByteSize.FromBytes(_searcher.Statistics.BytesRead).ToString("#.#")} read in {Stopwatch.GetElapsedTime(start)}.");
                    }
                    AnsiConsole.Write(new Rule());
                }
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
        var fileFmt = $"{foundResult.FilePath}";
        if (foundResult is MatchedResult m)
        {
            AnsiConsole.WriteLine($"{fileFmt} : Matches @ {string.Join(" - ", m.Matches.Select(x => { return $"[{x.MatchBegin}, {x.MatchEnd}]"; }))}");
        }
        else
        {
            AnsiConsole.WriteLine(fileFmt);
        }
    }

    private Channel<FoundResult> _channelResults;
    private Channel<SearchResult> _fullTextSearchRequests;

    private async Task FullTextSearch(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested && await _fullTextSearchRequests.Reader.WaitToReadAsync(ct))
            {
                var val = await _fullTextSearchRequests.Reader.ReadAsync(ct);
                var fi = new FileInfo(val.FullFilePath);
                if(fi.Length > _options.SizeBytes) continue;
                var matches = await _searcher.DoesFileMatch(val.FullFilePath, _options.MatchPattern, ct);
                if (matches.Any())
                {
                    await _channelResults.Writer.WriteAsync(new MatchedResult(val.RelFilePath, matches.ToArray()), ct);
                }
            }
        }
        catch (FileNotFoundException){}
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
                    await _threadReturns.WaitAsync(ct);
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