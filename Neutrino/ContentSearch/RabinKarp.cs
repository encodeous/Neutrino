using System.Runtime.CompilerServices;
using Neutrino.Utils;

namespace Neutrino.ContentSearch;

public class RabinKarp
{
    private readonly Hasher _hasher;
    private CircularBuffer<long> _buffer;
    private CircularBuffer<byte> _rawBuffer;

    public RabinKarp(int maxLength, Hasher hasher)
    {
        _hasher = hasher;
        _buffer = new CircularBuffer<long>(maxLength + 1);
        _rawBuffer = new CircularBuffer<byte>(maxLength + 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public void AddByte(byte val)
    {
        var cVal = _hasher.UpdateHash(_buffer.FBack(), val);
        _buffer.PushBack(cVal);
        _rawBuffer.PushBack(val);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public bool Matches(MatchContext ctx, SearchKey key)
    {
        if(key.Key.Length > _rawBuffer.Size) return false;
        int len = key.Key.Length;
        int first = _buffer.Size - key.Offset - 1;
        int last = first - len;
        if (len > 5)
        {
            long adjHash;
            if (last == -1)
            {
                adjHash = _buffer.FGet(first);
            }
            else
            {
                adjHash = (_buffer.FGet(first) - _buffer.FGet(last) + _hasher.Modulo) % _hasher.Modulo;
            }
            if(key.Hash != adjHash) return false;
            ctx.Collisions++;
        }
        for(int i = 0; i < len; i++)
        {
            if(key.Key[i] != _rawBuffer.FGet(last + i + 1)) return false;
        }

        if (len > 5)
        {
            ctx.Collisions--;
        }
        return true;
    }
}