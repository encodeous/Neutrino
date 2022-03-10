namespace Neutrino.ContentSearch;

public class MatchContextBuilder
{
    public long StartingIndex { get; set; } = 0;
    public List<ContentFilter> Filters { get; set; } = new();

    /// <summary>
    /// Fixes the starting point of the pattern to be a specific location in the file.
    /// </summary>
    /// <param name="start">Starting location</param>
    /// <returns></returns>
    public MatchContextBuilder WithStart(long start)
    {
        StartingIndex = start;
        return this;
    }
    
    /// <summary>
    /// Adds a filter to the context.
    /// </summary>
    /// <param name="filter"></param>
    /// <returns></returns>
    public MatchContextBuilder WithFilter(ContentFilter filter)
    {
        Filters.Add(filter);
        return this;
    }
    
    /// <summary>
    /// Adds filters to the context.
    /// </summary>
    /// <param name="filters"></param>
    /// <returns></returns>
    public MatchContextBuilder WithFilters(IEnumerable<ContentFilter> filters)
    {
        Filters.AddRange(filters);
        return this;
    }

    /// <summary>
    /// Parses filters from a string
    /// </summary>
    /// <param name="filter"></param>
    /// <returns></returns>
    public MatchContextBuilder WithFilterString(string filter)
    {
        return this;
    }
    
    public MatchContext Build(Hasher hasher)
    {
        return new MatchContext(StartingIndex, Filters, hasher);
    }
}