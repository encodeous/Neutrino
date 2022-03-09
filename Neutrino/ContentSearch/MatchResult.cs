namespace Neutrino.ContentSearch;

public record struct MatchResult(long MatchBegin, long MatchEnd)
{
    public long GetLength()
    {
        return MatchEnd - MatchBegin + 1;
    }
}