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

    public override void Initialize(Hasher hasher, int offset)
    {
        Key = new SearchKey(Value, hasher, offset);
    }

    public override void Increment(long curIndex)
    {
        Key.Increment(curIndex);
    }

    public override long GetRealLength()
    {
        return Value.Length;
    }

    public override MatchResult? MoveNextByte(RabinKarp karp, MatchContext context)
    {
        var index = context._curIndex;
        var lastIndex = context._lastIndex;
        var tLength = index - lastIndex;
        if (tLength < Length) return null;
        if (IsMatch(karp))
        {
            return new MatchResult(index - Key.Key.Length + 1, index);
        }
        return null;
    }
}