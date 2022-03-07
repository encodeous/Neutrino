using System.Collections.Concurrent;
using System.IO.Enumeration;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using DotNet.Globbing;

namespace Neutrino.Searcher;

public class Searcher
{
    public SearcherIndex Index { get; }
    public Glob NameMatcher { get; }
    public SearchOptions Options { get; }

    public Searcher(DirectoryInfo directory, SearchOptions options, SearcherIndex? index = null)
    {
        Index = index ?? new SearcherIndex(directory);
        NameMatcher = Glob.Parse(options.FileNameGlob);
        Options = options;
        _enumerationOptions = new EnumerationOptions()
        {
            AttributesToSkip = FileAttributes.System
        };
    }

    private ConcurrentDictionary<int, Task> _activeTasks;
    private int _taskId;
    private Channel<SearchRequest> RequestQueue;
    private Channel<SearchResult> ResultQueue;
    private EnumerationOptions _enumerationOptions;

    public async IAsyncEnumerable<SearchResult> SearchAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        RequestQueue = Channel.CreateUnbounded<SearchRequest>();
        ResultQueue = Channel.CreateBounded<SearchResult>(10000);
        _activeTasks = new ConcurrentDictionary<int, Task>();
        _taskId = 0;
        ct.Register(() =>
        {
            RequestQueue.Writer.Complete();
            ResultQueue.Writer.Complete();
        });
        await RequestQueue.Writer.WriteAsync(new SearchRequest()
        {
            FolderName = Index.RootDirectory.FullName,
            SearchDepth = 0
        }, ct);
        StartSearcher(ct);
        await foreach (var result in ResultQueue.Reader.ReadAllAsync())
        {
            yield return result;
        }
    }

    private void StartSearcher(CancellationToken ct)
    {
        int id = _taskId++;
        _activeTasks[id] = Task.Run(() => ExecuteSearch(id, ct));
    }
    private async Task ExecuteSearch(int id, CancellationToken ct = default)
    {
        var curState = new SearcherState(new List<SearchRequest>(), new List<SearchResult>());
        while (!ct.IsCancellationRequested && !RequestQueue.Reader.Completion.IsCompleted)
        {
            var val = await RequestQueue.Reader.ReadAsync();
            curState.Depth = val.SearchDepth;

            FileSystemEnumerator<object> ffe = new FastFileEnumerator<SearcherState>(val.FolderName, curState, ResultHandler, _enumerationOptions);
            while (ffe.MoveNext()) {} ;
            foreach (var result in curState.Results)
            {
                await ResultQueue.Writer.WriteAsync(result, ct);
            }
            foreach (var req in curState.Requests)
            {
                await RequestQueue.Writer.WriteAsync(req, ct);
            }

            try
            {
                if (curState.Requests.Count == 0 && RequestQueue.Reader.Count == 0)
                {
                    _activeTasks.Remove(id, out _);
                    if (_activeTasks.Count == 0)
                    {
                        RequestQueue.Writer.Complete();
                        ResultQueue.Writer.Complete();
                    }
                    return;
                }
                while(_activeTasks.Count < Options.Concurrency && RequestQueue.Reader.Count > Options.Concurrency * 4)
                {
                    StartSearcher(ct);
                }
            }
            finally
            {
                curState.Requests.Clear();
                curState.Results.Clear();
            }
        }
    }
    
    void ResultHandler(FileSystemEntry res, SearcherState data)
    {
        var fullPath = res.ToFullPath();
        if (res.IsDirectory && data.Depth + 1 < Options.MaxDepth)
        {
            data.Requests.Add(new SearchRequest() { FolderName = fullPath, SearchDepth = data.Depth + 1});
        }
        else
        {
            if (NameMatcher.IsMatch(fullPath))
            {
                data.Results.Add(new SearchResult() { FullFilePath = fullPath, RelFilePath = res.FileName.ToString() });
            }
        }
    }

    public record struct SearcherState(List<SearchRequest> Requests, List<SearchResult> Results, int Depth = 0);
}