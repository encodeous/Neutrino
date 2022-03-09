using System.Runtime.CompilerServices;

namespace Neutrino.ContentSearch.Filters;

public class AnyFixedFilter : ContentFilter
{
    public AnyFixedFilter(long fixedLength) : base(FilterType.AnyFixed)
    {
        Length = fixedLength;
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
        return new MatchResult(context._curIndex - Length + 1, context._curIndex, true);
    }
}