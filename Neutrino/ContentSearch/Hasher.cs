using System.Runtime.CompilerServices;

namespace Neutrino.ContentSearch;

public class Hasher
{
    public long CurrentBase = 1;
    public long Index = 0;

    public Hasher(long basePrime, long mod)
    {
        Prime = basePrime;
        Modulo = mod;
    }

    public void Reset()
    {
        CurrentBase = 1;
        Index = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public void Increment()
    {
        CurrentBase = (CurrentBase * Prime) % Modulo;
        Index++;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public long UpdateHash(long oldHash, byte newData)
    {
        return (oldHash + ((newData + 1) * CurrentBase) % Modulo) % Modulo;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public long UpdateKey(long oldHash)
    {
        return (oldHash * Prime) % Modulo;
    }

    public long Prime {get; private set; }
    public long Modulo {get; private set; }
}