namespace Neutrino.BinarySearch;

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

    public void Increment()
    {
        CurrentBase = (CurrentBase * Prime) % Modulo;
        Index++;
    }

    public long UpdateHash(long oldHash, byte newData)
    {
        return (oldHash + ((newData + 1) * CurrentBase) % Modulo) % Modulo;
    }

    public long Prime {get; private set; }
    public long Modulo {get; private set; }
}