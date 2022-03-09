namespace Neutrino.ContentSearch.Filters;

public class EqualFilter : HashedFilter
{
    public EqualFilter(byte[] value) : base(FilterType.Equals, value)
    {
    }

    public override bool IsMatch(RabinKarp karp)
    {
        return karp.Matches(Key);
    }
}