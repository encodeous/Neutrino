using System.Collections.Concurrent;
using System.IO.Enumeration;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using DotNet.Globbing;
using Neutrino.Utils;

namespace Neutrino.Searcher;

public class FileSearcher
{
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
    
    private EnumerationOptions _enumerationOptions;

    public IEnumerable<SearchResult> Search([EnumeratorCancellation] CancellationToken ct = default)
    {
        var requests = new Stack<SearchRequest>();
        
        var curState = new SearcherState(new List<SearchRequest>(), new List<SearchResult>());
        requests.Push(new SearchRequest()
        {
            FolderName = RootDirectory.FullName,
            SearchDepth = 0
        });
        while (!ct.IsCancellationRequested && requests.Count != 0)
        {
            var val = requests.Pop();
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
            foreach(var req in curState.Requests)
            {
                requests.Push(req);
            }
            foreach (var res in curState.Results)
            {
                yield return res;
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

    public record SearcherState(List<SearchRequest> Requests, List<SearchResult> Results)
    {
        public int Depth = 0;
    }
}