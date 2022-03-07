using System.IO.Enumeration;

namespace Neutrino;

public class FastFileEnumerator<T> : FileSystemEnumerator<object>
{
    private readonly T _data;
    private readonly ResultDelegate _resultHandler;

    public delegate void ResultDelegate(FileSystemEntry res, T data);
    
    public FastFileEnumerator(string directory, T data, ResultDelegate resultHandler, EnumerationOptions? options = null) : base(directory, options)
    {
        _data = data;
        _resultHandler = resultHandler;
    }

    protected override object TransformEntry(ref FileSystemEntry entry)
    {
        _resultHandler.Invoke(entry, _data);
        return null;
    }
}