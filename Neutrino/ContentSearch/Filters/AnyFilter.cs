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

    public override bool IsMatch(MatchContext ctx, RabinKarp karp)
    {
        return true;
    }

    public override MatchResult MoveNextByte(RabinKarp karp, MatchContext context)
    {
        context._filters.Dequeue();
        context._isWildcard = true;
        return new MatchResult(0, 0, false);
    }
}