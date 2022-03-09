namespace Neutrino.ContentSearch.Filters;

public class NotEqualFilter : HashedFilter
{
    public NotEqualFilter(byte[] value) : base(FilterType.NotEquals, value)
    {
    }

    public override bool IsMatch(RabinKarp karp)
    {
        return !karp.Matches(Key);
    }
}