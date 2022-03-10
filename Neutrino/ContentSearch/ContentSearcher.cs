namespace Neutrino.ContentSearch;

public class ContentSearcher
{
    private Hasher _hasher;
    private HashSet<MatchContext> _contexts;
    private MatchContext[] _cachedContexts;
    private int _maxLength = 0;
    private RabinKarp _rabinKarp;
    /// <summary>
    /// The number of hash collisions
    /// </summary>
    public int Collisions
    {
        get
        {
            int cnt = 0;
            foreach (var matchContext in _cachedContexts)
            {
                cnt += matchContext.Collisions;
            }
            return cnt;
        }
    }

    public ContentSearcher()
    {
        _hasher = new Hasher(313, 1_000_000_007);
        _contexts = new();
    }

    public MatchContext AddPattern(MatchContextBuilder builder)
    {
        var ctx = builder.Build(_hasher);
        _contexts.Add(ctx);
        _maxLength = Math.Max(_maxLength, ctx.MaxMatchLength);
        return ctx;
    }

    public void Build()
    {
        if(_rabinKarp != null) throw new InvalidOperationException("Already built");
        _rabinKarp = new RabinKarp(_maxLength, _hasher);
        _cachedContexts = _contexts.ToArray();
    }

    public void AddByte(byte b)
    {
        if(_rabinKarp == null) throw new InvalidOperationException("Not built yet");
        _rabinKarp.AddByte(b);
        foreach(var k in _cachedContexts)
        {
            k.MoveNextByte(_rabinKarp, _hasher.Index);
        }
        _hasher.Increment();
    }
}