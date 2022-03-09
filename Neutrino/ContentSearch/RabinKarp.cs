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

    public void AddByte(byte val)
    {
        var cVal = _hasher.UpdateHash(_buffer.Size == 0 ? 0 : _buffer.Back(), val);
        if(_buffer.IsFull) _buffer.PopFront();
        if(_rawBuffer.IsFull) _rawBuffer.PopFront();
        _buffer.PushBack(cVal);
        _rawBuffer.PushBack(val);
    }

    public bool Matches(SearchKey key)
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
                adjHash = _buffer[first];
            }
            else
            {
                adjHash = (_buffer[first] - _buffer[last] + _hasher.Modulo) % _hasher.Modulo;
            }
            if(key.Hash != adjHash) return false;
        }
        for(int i = 0; i < len; i++)
        {
            if(key.Key[i] != _rawBuffer[last + i + 1]) return false;
        }

        return true;
    }
}