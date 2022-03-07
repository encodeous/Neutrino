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
        var cVal = _hasher.UpdateHash(_buffer.IsEmpty ? 0 : _buffer.Back(), val);
        if(_buffer.IsFull) _buffer.PopFront();
        if(_rawBuffer.IsFull) _rawBuffer.PopFront();
        _buffer.PushBack(cVal);
        _rawBuffer.PushBack(val);
    }

    public bool Matches(SearchKey key)
    {
        if(key.Key.Length > _rawBuffer.Size) return false;
        int len = key.Key.Length;
        long adjHash;
        if (key.Key.Length == _rawBuffer.Size)
        {
            adjHash = _buffer.Back();
        }
        else
        {
            adjHash = (_buffer.Back() - _buffer[_buffer.Size - len - 1] + _hasher.Modulo) % _hasher.Modulo;
        }
        if(key.Hash != adjHash) return false;
        for(int i = 0; i < len; i++)
        {
            if(key.Key[i] != _rawBuffer[_rawBuffer.Size - len + i]) return false;
        }

        return true;
    }
}