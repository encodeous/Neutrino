namespace Neutrino;

public record SearchOptions(
    string FileNameGlob,
    bool TraverseSymlinks = false,
    int MaxDepth = Int32.MaxValue,
    int Concurrency = 3
    );