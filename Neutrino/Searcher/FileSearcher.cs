using System.Collections.Concurrent;
using System.IO.Enumeration;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using DotNet.Globbing;
using Neutrino.Utils;

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
    public ParallelStack<SearchRequest> Processor;
    public Channel<SearchResult> ResultQueue;
    private EnumerationOptions _enumerationOptions;

    public async IAsyncEnumerable<SearchResult> SearchAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        Processor = new ParallelStack<SearchRequest>(Options.Concurrency, ChannelBound, ct);
        ResultQueue = Channel.CreateBounded<SearchResult>(ChannelBound);
        var completion = () =>
        {
            Processor.SharedChannel.Writer.TryComplete();
            ResultQueue.Writer.TryComplete();
        };
        _taskId = 0;
        Processor.Token.Register(completion);
        ct.Register(completion);
        await Processor.SharedChannel.Writer.WriteAsync(new SearchRequest()
        {
            FolderName = RootDirectory.FullName,
            SearchDepth = 0
        }, ct);
        for (int i = 0; i < Options.Concurrency; i++)
        {
            var t = Processor.AssignThread();
            Task.Run(() => ExecuteSearch(t, ct), ct);
        }
        await foreach (var result in ResultQueue.Reader.ReadAllAsync())
        {
            yield return result;
        }
    }
    private async Task ExecuteSearch(ParallelStack<SearchRequest>.StackAccessor accessor, CancellationToken ct = default)
    {
        var curState = new SearcherState(new List<SearchRequest>(), new List<SearchResult>());
        while (!ct.IsCancellationRequested && !accessor.Stack.IsClosed)
        {
            var val = await accessor.Pop();
            curState.Depth = val.SearchDepth;

            try
            {
                FileSystemEnumerator<object> ffe = new FastFileEnumerator<SearcherState>(val.FolderName, curState, ResultHandler, _enumerationOptions);
                while (ffe.MoveNext()) {}
            }
            catch
            {
                // ignored
            }
            foreach (var result in curState.Results)
            {
                await ResultQueue.Writer.WriteAsync(result, ct);
            }
            foreach (var req in curState.Requests)
            {
                await accessor.Push(req);
            }
            curState.Requests.Clear();
            curState.Results.Clear();
        }
    }

    void ResultHandler(FileSystemEntry res, SearcherState data)
    {
        string fullPath = res.ToFullPath();
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