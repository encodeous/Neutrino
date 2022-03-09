namespace Neutrino.ContentSearch.Filters;

public sealed class EqualFilter : HashedFilter
{
    public EqualFilter(byte[] value) : base(FilterType.Equals, value)
    {
    }

    public override bool IsMatch(MatchContext ctx, RabinKarp karp)
    {
        return karp.Matches(ctx, Key);
    }
}