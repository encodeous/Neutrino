using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.IO.Enumeration;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using DotNet.Globbing;
using Neutrino.ContentSearch;
using Neutrino.Utils;

namespace Neutrino.Searcher;

public class FileSearcher
{
    public DirectoryInfo RootDirectory { get; }
    public Glob NameMatcher { get; }
    public SearcherParameters Options { get; }
    public NeutrinoStats Statistics { get; private set; }

    public FileSearcher(DirectoryInfo directory, SearcherParameters options)
    {
        RootDirectory = directory;
        NameMatcher = Glob.Parse(options.FileNameGlob);
        Options = options;
        _enumerationOptions = new EnumerationOptions()
        {
            AttributesToSkip = FileAttributes.System
        };
        Statistics = new NeutrinoStats();
    }
    
    private EnumerationOptions _enumerationOptions;

    public IEnumerable<SearchResult> Search(CancellationToken ct = default)
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

    public async Task<ReadOnlyCollection<MatchResult>> DoesFileMatch(string filePath, string filterString, CancellationToken ct)
    {
        FileStream fs;
        try
        {
            fs = File.OpenRead(filePath);
        }
        catch
        {
            // ignored
            return new ReadOnlyCollection<MatchResult>(ArraySegment<MatchResult>.Empty);
        }
        var buf = ArrayPool<byte>.Shared.Rent(8192);
        try
        {
            var builder = new MatchContextBuilder()
                .WithFilterString(filterString);
            var fts = new ContentSearcher();
            var ctx = fts.AddPattern(builder);
            fts.Build();
            int len;
            while ((len = await fs.ReadAsync(buf, ct)) != 0)
            {
                for (int i = 0; i < len && !ct.IsCancellationRequested; i++)
                {
                    if (!fts.AddByte(buf[i]))
                    {
                        goto end_match;
                    }
                }

                Interlocked.Add(ref Statistics._bytesRead, len);
            }

            end_match:
            if (ctx.IsMatch)
            {
                Interlocked.Increment(ref Statistics._objectsContentMatched);
                return ctx.Results;
            }
            return new ReadOnlyCollection<MatchResult>(ArraySegment<MatchResult>.Empty);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buf);
        }
    }

    void ResultHandler(FileSystemEntry res, SearcherState data)
    {
        try
        {
            var fullPath = res.ToFullPath();
            var relPath = Path.GetRelativePath(RootDirectory.FullName, fullPath);
            Statistics._objectsDiscovered++;
            if (res.IsDirectory && data.Depth + 1 < Options.MaxDepth)
            {
                data.Requests.Add(new SearchRequest() { FolderName = fullPath, SearchDepth = data.Depth + 1});
            }
            else
            {
                if (NameMatcher.IsMatch(relPath))
                {
                    Statistics._objectsGlobMatched++;
                    data.Results.Add(new SearchResult() { FullFilePath = fullPath, RelFilePath = relPath });
                }
            }
        }
        catch (Exception)
        {
            // ignored
        }
    }

    public record SearcherState(List<SearchRequest> Requests, List<SearchResult> Results)
    {
        public int Depth = 0;
    }
}