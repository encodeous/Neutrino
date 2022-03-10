using Neutrino.ContentSearch;

namespace Neutrino.Cli.Data;

public record MatchedResult(string FilePath, MatchResult[] Matches) : FoundResult(FilePath);