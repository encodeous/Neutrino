using System.Runtime.CompilerServices;

namespace Neutrino.ContentSearch;

public class SearchKey
{
    private readonly Hasher _hasher;
    public byte[] Key { get; private set; }
    public long Hash { get; private set; }
    private long prevIndex = -1;
    public int Offset { get; private set; }
    
    public SearchKey(byte[] key, Hasher hasher, int offset = 0)
    {
        Offset = offset;
        _hasher = hasher;
        Key = key;
        if(key.Length <= 5) return;
        if(hasher.Index != 0) throw new ArgumentException("hasher.Index must be 0");
        foreach (var b in key)
        {
            Hash = _hasher.UpdateHash(Hash, b);
            _hasher.Increment();
        }
        _hasher.Reset();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Increment(long currentIndex)
    {
        if(Key.Length <= 5) return;
        currentIndex -= Offset;
        if (prevIndex == currentIndex) return;
        prevIndex = currentIndex;
        if(currentIndex < Key.Length)
        {
            return;
        }

        Hash = _hasher.UpdateKey(Hash);
    }
}