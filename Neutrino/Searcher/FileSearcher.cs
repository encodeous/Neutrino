using System.Collections.Concurrent;
using System.IO.Enumeration;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using DotNet.Globbing;

namespace Neutrino.Searcher;

public class FileSearcher
{
    public const int ChannelBound = 10000;
    public DirectoryInfo RootDirectory { get; }
    public Glob NameMatcher { get; }
    public SearcherParameters Options { get; }

    public FileSearcher(DirectoryInfo directory, SearcherParameters options)
    {
        RootDirectory = directory;
        NameMatcher = Glob.Parse(options.FileNameGlob);
        Options = options;
        _enumerationOptions = new EnumerationOptions()
        {
            AttributesToSkip = FileAttributes.System
        };
    }
    
    private int _taskId;
    private volatile int _waitingThreads;
    public Channel<SearchRequest> RequestQueue;
    public Channel<SearchResult> ResultQueue;
    private EnumerationOptions _enumerationOptions;

    public async IAsyncEnumerable<SearchResult> SearchAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        RequestQueue = Channel.CreateUnbounded<SearchRequest>();
        ResultQueue = Channel.CreateBounded<SearchResult>(ChannelBound);
        _taskId = 0;
        ct.Register(() =>
        {
            RequestQueue.Writer.TryComplete();
            ResultQueue.Writer.TryComplete();
        });
        await RequestQueue.Writer.WriteAsync(new SearchRequest()
        {
            FolderName = RootDirectory.FullName,
            SearchDepth = 0
        }, ct);
        for (int i = 0; i < Options.Concurrency; i++)
        {
            StartSearcher(ct);
        }
        Task.Run(async () =>
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(100);
                if (RequestQueue.Reader.Count == 0 && _waitingThreads == Options.Concurrency)
                {
                    ResultQueue.Writer.TryComplete();
                    RequestQueue.Writer.TryComplete();
                }
            }
        });
        await foreach (var result in ResultQueue.Reader.ReadAllAsync())
        {
            yield return result;
        }
    }

    private void StartSearcher(CancellationToken ct)
    {
        Task.Run(() => ExecuteSearch(ct), ct);
    }
    private async Task ExecuteSearch(CancellationToken ct = default)
    {
        var curState = new SearcherState(new List<SearchRequest>(), new List<SearchResult>());
        while (!ct.IsCancellationRequested && !RequestQueue.Reader.Completion.IsCompleted)
        {
            Interlocked.Increment(ref _waitingThreads);
            var val = await RequestQueue.Reader.ReadAsync(ct);
            Interlocked.Decrement(ref _waitingThreads);
            curState.Depth = val.SearchDepth;

            try
            {
                FileSystemEnumerator<object> ffe = new FastFileEnumerator<SearcherState>(val.FolderName, curState, ResultHandler, _enumerationOptions);
                while (ffe.MoveNext()) {}
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            foreach (var result in curState.Results)
            {
                await ResultQueue.Writer.WriteAsync(result, ct);
            }
            foreach (var req in curState.Requests)
            {
                await RequestQueue.Writer.WriteAsync(req, ct);
            }
            curState.Requests.Clear();
            curState.Results.Clear();
        }
    }
    
    void ResultHandler(FileSystemEntry res, SearcherState data)
    {
        var fullPath = res.ToFullPath();
        var relPath = Path.GetRelativePath(RootDirectory.FullName, fullPath);
        if (res.IsDirectory && data.Depth + 1 < Options.MaxDepth)
        {
            data.Requests.Add(new SearchRequest() { FolderName = fullPath, SearchDepth = data.Depth + 1});
        }
        else
        {
            if (NameMatcher.IsMatch(relPath))
            {
                data.Results.Add(new SearchResult() { FullFilePath = fullPath, RelFilePath = relPath });
            }
        }
    }

    public record struct SearcherState(List<SearchRequest> Requests, List<SearchResult> Results, int Depth = 0);
}