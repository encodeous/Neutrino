namespace Neutrino.ContentSearch.Filters;

public abstract class HashedFilter : ContentFilter
{
    public byte[] Value { get; private set; }
    public SearchKey Key { get; internal set; }
    
    internal HashedFilter(FilterType type, byte[] value) : base(type)
    {
        Value = value;
        Length = value.Length;
    }

    public sealed override void Initialize(Hasher hasher, int offset)
    {
        Key = new SearchKey(Value, hasher, offset);
        Keys = new[] { Key };
    }

    public sealed override long GetRealLength()
    {
        return Value.Length;
    }

    public sealed override MatchResult MoveNextByte(RabinKarp karp, MatchContext context)
    {
        var index = context._curIndex;
        var lastIndex = context._lastIndex;
        var tLength = index - lastIndex;
        if (tLength < Length) return new MatchResult(0, 0, false);
        if (IsMatch(context, karp))
        {
            return new MatchResult(index - Key.Key.Length + 1, index, true);
        }
        return new MatchResult(0, 0, false);
    }
}