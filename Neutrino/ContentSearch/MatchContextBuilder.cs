using System.Text;

namespace Neutrino.ContentSearch;

public class MatchContextBuilder
{
    public List<ContentFilter> Filters { get; set; } = new();

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
    /// <param name="encoding">Defaults to UTF-8</param>
    /// <returns></returns>
    public MatchContextBuilder WithFilterString(string filter, Encoding? encoding = null)
    {
        Filters.AddRange(PatternParser.Parse(filter, encoding ?? Encoding.UTF8));
        return this;
    }
    
    public MatchContext Build(Hasher hasher)
    {
        return new MatchContext(Filters, hasher);
    }
}