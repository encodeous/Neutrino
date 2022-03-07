using System.Collections.ObjectModel;

namespace Neutrino.ContentSearch;

public class MatchContext
{
    public MatchContext(long startingIndex, IReadOnlyList<ContentFilter> filters, Hasher hasher)
    {
        StartingIndex = startingIndex;
        _waitUntil = startingIndex;
        _filters = new Queue<ContentFilter>(filters);
        foreach (var filter in _filters)
        {
            if(filter.Key == null)
            {
                if (filter.Type is ContentFilter.FilterType.Equals or ContentFilter.FilterType.NotEquals)
                {
                    filter.Key = new SearchKey(filter.Value, hasher);
                }
            }
        }
    }

    public long StartingIndex { get; }
    private Queue<ContentFilter> _filters { get; }
    public ReadOnlyCollection<MatchResult> Results => _results.AsReadOnly();
    public bool IsComplete => _filters.Count == 0;
    public bool IsMatch => _filters.Count == 0 && _isMatch;
    public List<MatchResult> _results { get; } = new();

    private bool _isMatch = true;
    
    private long _waitUntil = 0;

    internal void MoveNextByte(RabinKarp karp, long curIndex)
    {
        foreach (var filter in _filters)
        {
            filter.Key?.Increment(curIndex);
        }
        if(curIndex < _waitUntil)
        {
            return;
        }

        if (_filters.Count == 0) return;
        var cfilter = _filters.First();
        if (cfilter.Type == ContentFilter.FilterType.Equals)
        {
            if (karp.Matches(cfilter.Key))
            {
                _results.Add(new MatchResult(curIndex - cfilter.Length + 1, curIndex));
                _filters.Dequeue();
                if(_filters.Count != 0) _waitUntil = _filters.First().Length + curIndex;
            }
        }
        else if (cfilter.Type == ContentFilter.FilterType.NotEquals)
        {
            if (!karp.Matches(cfilter.Key))
            {
                _results.Add(new MatchResult(curIndex - cfilter.Length + 1, curIndex));
                _filters.Dequeue();
                if(_filters.Count != 0) _waitUntil = _filters.First().Length + curIndex;
            }
        }
        else if (cfilter.Type == ContentFilter.FilterType.NotEquals)
        {
            if (!karp.Matches(cfilter.Key))
            {
                _results.Add(new MatchResult(curIndex - cfilter.Length + 1, curIndex));
                _filters.Dequeue();
                if(_filters.Count != 0) _waitUntil = _filters.First().Length + curIndex;
            }
        }
    }
}