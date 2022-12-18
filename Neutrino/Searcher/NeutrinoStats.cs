namespace Neutrino.Searcher;

public class NeutrinoStats
{
    internal long _bytesRead;
    internal long _objectsContentMatched;
    internal long _objectsGlobMatched;
    internal long _objectsDiscovered;

    public long BytesRead => _bytesRead;

    public long ObjectsContentMatched => _objectsContentMatched;
    public long ObjectsGlobMatched => _objectsGlobMatched;

    public long ObjectsDiscovered => _objectsDiscovered;
}