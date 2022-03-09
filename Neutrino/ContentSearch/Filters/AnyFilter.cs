namespace Neutrino.ContentSearch.Filters;

public class AnyFilter : ContentFilter
{
    public AnyFilter() : base(FilterType.Any)
    {
    }

    public override long GetRealLength()
    {
        return 0;
    }

    public override void Initialize(Hasher hasher, int offset)
    {
        
    }

    public override void Increment(long curIndex)
    {
        
    }

    public override bool IsMatch(RabinKarp karp)
    {
        return true;
    }

    public override MatchResult? MoveNextByte(RabinKarp karp, MatchContext context)
    {
        context._filters.Dequeue();
        context._isWildcard = true;
        return null;
    }
}