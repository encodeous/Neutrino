namespace Neutrino.ContentSearch;

public class ContentSearcher
{
    private Hasher _hasher;
    private Dictionary<string, MatchContext> _contextMap;
    private HashSet<MatchContext> _contexts;
    public IReadOnlyDictionary<string, MatchContext> Contexts => _contextMap;
    private int _maxLength = 0;
    private RabinKarp _rabinKarp;
    public ContentSearcher()
    {
        _hasher = new Hasher(313, 1_000_000_007);
        _contextMap = new ();
        _contexts = new();
    }

    public MatchContext GetContext(string context)
    {
        return _contextMap[context];
    }
    
    public MatchContext AddPattern(string name, MatchContextBuilder builder)
    {
        var ctx = builder.Build(_hasher);
        _contexts.Add(ctx);
        _contextMap[name] = ctx;
        _maxLength = Math.Max(_maxLength, ctx.MaxMatchLength);
        return ctx;
    }

    public void Build()
    {
        if(_rabinKarp != null) throw new InvalidOperationException("Already built");
        _rabinKarp = new RabinKarp(_maxLength, _hasher);
    }

    public void AddByte(byte b)
    {
        if(_rabinKarp == null) throw new InvalidOperationException("Not built yet");
        _rabinKarp.AddByte(b);
        foreach(var k in _contexts)
        {
            k.MoveNextByte(_rabinKarp, _hasher.Index);
        }
        _hasher.Increment();
    }
}