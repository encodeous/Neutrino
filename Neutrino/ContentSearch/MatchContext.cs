using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using Neutrino.ContentSearch.Filters;

namespace Neutrino.ContentSearch;

public class MatchContext
{
    public MatchContext(IReadOnlyList<ContentFilter> filters, Hasher hasher)
    {
        _filters = new Queue<ContentFilter>();
        var tmp = new Queue<ContentFilter>();
        ContentFilter fi;
        foreach (var filter in filters)
        {
            if (filter is AnyFilter)
            {
                if (tmp.Count != 0)
                {
                    if (tmp.Count > 1)
                    {
                        fi = new CompoundFilter(tmp);
                    }
                    else
                    {
                        fi = tmp.First();
                    }
                    tmp.Clear();
                    fi.Initialize(hasher, 0);
                    _filters.Enqueue(fi);
                }
                filter.Initialize(hasher, 0);
                _filters.Enqueue(filter);
            }
            else
            {
                if(filter.Length == 0) continue;
                tmp.Enqueue(filter);
            }
        }
        if (tmp.Count != 0)
        {
            if (tmp.Count > 1)
            {
                fi = new CompoundFilter(tmp);
                    
            }
            else
            {
                fi = tmp.First();
            }
            tmp.Clear();
            fi.Initialize(hasher, 0);
            _filters.Enqueue(fi);
        }

        foreach (var filter in _filters)
        {
            MaxMatchLength = Math.Max(MaxMatchLength, (int)filter.GetRealLength());
        }

        if (_filters.Count == 1 && _filters.First() is AnyFilter)
        {
            _filters.Clear();
        }
        _filterCache = _filters.ToArray();
    }

    internal int Collisions;
    internal Queue<ContentFilter> _filters { get; }
    internal ContentFilter[] _filterCache { get; private set; }
    public ReadOnlyCollection<MatchResult> Results => _results.AsReadOnly();
    public bool IsComplete => _filters.Count == 0;
    public bool IsMatch => _filters.Count == 0 && _isMatch;
    internal int MaxMatchLength = 0;
    private List<MatchResult> _results { get; } = new();

    private bool _isMatch = true;
    internal bool _repeatCurrentByte = false;

    internal long _lastIndex = -1;
    internal long _curIndex;
    internal bool _isWildcard;
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    internal void MoveNextByte(RabinKarp karp, long curIndex)
    {
        if (!_isMatch || _filters.Count == 0) return;
        _curIndex = curIndex;
        foreach (var filter in _filterCache)
        {
            foreach (var key in filter.Keys)
            {
                key.Increment(curIndex);
            }
        }

        ApplyFilter:
        var curFilter = _filters.Peek();
        if (!ContentFilter.IsInBounds(curFilter.Length, this)) return;
        var res = curFilter.MoveNextByte(karp, this);

        if (_repeatCurrentByte && _filters.Count != 0)
        {
            _repeatCurrentByte = false;
            goto ApplyFilter;
        }
        
        if (res.IsSuccess)
        {
            _filters.Dequeue();
            _filterCache = _filters.ToArray();
            if (_isWildcard)
            {
                _results.Add(new MatchResult(_lastIndex, res.MatchBegin - 1, true));
            }

            if (curFilter is CompoundFilter cf)
            {
                long startIndex = curIndex - cf.Length + 1;
                foreach (var cmpFilter in cf._filters)
                {
                    _results.Add(new MatchResult(startIndex, startIndex + cmpFilter.Length - 1, true));
                    startIndex += cmpFilter.Length;
                }
            }
            else
            {
                _results.Add(res);
            }
            _lastIndex = curIndex;
            _isWildcard = false;
        }
        else
        {
            if (!_isWildcard)
            {
                _isMatch = false;
            }
        }
    }
}