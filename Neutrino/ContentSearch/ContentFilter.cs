namespace Neutrino.ContentSearch;

public class ContentFilter
{
    public FilterType Type { get; private set; }
    public long Length { get; private set; }
    public byte[] Value { get; private set; }
    internal ContentFilter(){}
    public SearchKey? Key { get; internal set; }
    

    public static ContentFilter CreateEquals(byte[] value)
    {
        return new ContentFilter()
        {
            Type = FilterType.Equals,
            Length = value.Length,
            Value = value
        };
    }

    public static ContentFilter CreateNotEquals(byte[] value)
    {
        return new ContentFilter()
        {
            Type = FilterType.NotEquals,
            Length = value.Length,
            Value = value,
        };
    }

    public enum FilterType
    {
        Equals,
        NotEquals
    }
}