namespace Neutrino.ContentSearch;

public record struct MatchResult(long MatchBegin, long MatchEnd, bool IsSuccess);