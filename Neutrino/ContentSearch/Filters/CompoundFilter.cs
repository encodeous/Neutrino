﻿using System.Runtime.CompilerServices;

namespace Neutrino.ContentSearch.Filters;

internal class CompoundFilter : ContentFilter
{
    internal ContentFilter[] _filters;
    private long realLength = 0;
    
    public CompoundFilter(IReadOnlyCollection<ContentFilter> filters) : base(FilterType.Compound)
    {
        long firstReal = long.MaxValue;
        long cur = 0;
        foreach (var filter in filters)
        {
            if(filter is CompoundFilter)
                throw new ArgumentException("CompoundFilter cannot contain another CompoundFilter");
            if (filter.GetRealLength() != 0)
            {
                firstReal = Math.Min(firstReal, cur);
            }
            cur += filter.Length;
        }

        Length = cur;
        realLength = cur - firstReal;
        _filters = filters.ToArray();
    }

    public override long GetRealLength()
    {
        return realLength;
    }

    public override void Initialize(Hasher hasher, int offset)
    {
        int cOffset = 0;
        for (int i = _filters.Length - 1; i >= 0; i--)
        {
            _filters[i].Initialize(hasher, cOffset);
            cOffset += (int)_filters[i].Length;
        }
    }

    public override void Increment(long curIndex)
    {
        foreach (var filter in _filters)
        {
            filter.Increment(curIndex);
        }
    }

    public override bool IsMatch(RabinKarp karp)
    {
        throw new NotImplementedException();
    }

    public override MatchResult? MoveNextByte(RabinKarp karp, MatchContext context)
    {
        bool matches = true;
        foreach (var filter in _filters)
        {
            if (!filter.IsMatch(karp))
            {
                matches = false;
                break;
            }
        }

        if (matches)
        {
            return new MatchResult(context._curIndex - Length + 1, context._curIndex);
        }

        return null;
    }
}