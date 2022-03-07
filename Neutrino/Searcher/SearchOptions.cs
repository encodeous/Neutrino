namespace Neutrino.Searcher;

public record SearchOptions(
    string FileNameGlob,
    int MaxDepth = Int32.MaxValue,
    int Concurrency = 3
    );