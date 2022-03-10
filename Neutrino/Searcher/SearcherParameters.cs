namespace Neutrino.Searcher;

public record SearcherParameters(
    string FileNameGlob,
    int MaxDepth = Int32.MaxValue,
    int Concurrency = 3
    );