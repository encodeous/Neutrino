namespace Neutrino.Cli.Data;

public record SearchOptions(
    string Glob,
    bool IsJsonOutput,
    int Concurrency,
    long SizeBytes,
    string MatchPattern,
    string Path
    );