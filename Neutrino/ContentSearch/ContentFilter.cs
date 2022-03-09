using System.Runtime.CompilerServices;

namespace Neutrino.ContentSearch;

public abstract class ContentFilter
{
    public FilterType Type { get; private set; }
    public long Length { get; protected set; }
    public abstract long GetRealLength();
    internal ContentFilter(FilterType type){ Type = type; }
    public abstract void Initialize(Hasher hasher, int offset);
    public abstract void Increment(long curIndex);
    public abstract bool IsMatch(RabinKarp karp);
    public abstract MatchResult? MoveNextByte(RabinKarp karp, MatchContext context);

    public enum FilterType
    {
        Equals,
        NotEquals,
        AnyFixed,
        Any,
        Compound
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsInBounds(long length, MatchContext context)
    {
        var index = context._curIndex;
        var lastIndex = context._lastIndex;
        var tLength = index - lastIndex;
        return tLength >= length;
    }
}