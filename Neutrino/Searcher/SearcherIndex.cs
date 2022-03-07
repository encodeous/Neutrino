namespace Neutrino.Searcher;

public class SearcherIndex
{
    public SearcherIndex(DirectoryInfo rootDirectory)
    {
        RootDirectory = rootDirectory;
    }

    public DirectoryInfo RootDirectory { get; }
}