using System;
using System.Collections.Generic;
using System.Text;
using Neutrino.ContentSearch;
using Neutrino.ContentSearch.Filters;
using Xunit;

namespace Neutrino.Tests;


public class FilterTests
{
    [Theory]
    [InlineData("Hello, World!", "Hello", true)]
    [InlineData("Hello, World!", "hello", false)]
    [InlineData("Hello, World!", "llo, Worl", true)]
    [InlineData("Hello, World!", "!", true)]
    [InlineData("Hello, World!", "\n", false)]
    [InlineData("sdfhasf8924rsdkjfhj892hkldsjhgggg", "2hk", true)]
    [InlineData("sdfhasf8924rsdkjfhj892hkldsjhgggg", "3hk", false)]
    [InlineData("sdfhasf8924rsdkjfhj892hkldsjhgggg!", "sdfhasf8924rsdkjfhj892hkldsjhgggg!", true)]
    [InlineData("sdfhasf8924rsdkjfhj892hkldsjhgggg", "", true)]
    [InlineData("", "", true)]
    [InlineData("abc", "a", true)]
    public void SimpleContainTest(string source, string contained, bool expected)
    {
        var builder = new MatchContextBuilder()
            .WithFilter(new AnyFilter())
            .WithFilter(new EqualFilter(Encoding.UTF8.GetBytes(contained)));
        var searcher = new ContentSearcher();
        var pat = searcher.AddPattern(builder);
        searcher.Build();
        var bytes = Encoding.UTF8.GetBytes(source);
        foreach (var b in bytes)
        {
            searcher.AddByte(b);
        }
        Assert.Equal(pat.IsMatch, expected);
    }
    
    [Theory]
    [InlineData("Hello, World!", "*'Hello'", true)]
    [InlineData("Hello, World!", "*'Hello, '!'World'", false)]
    [InlineData("Hello, 1World!", "*'Hello, '<1>!'World'", false)]
    [InlineData("<1!!!>sdasf\\'", "*'!!>sdasf\\\\''", true)]
    [InlineData("<1!!!>sdesf\\'", "*'!!>sdasf\\\\''", false)]
    [InlineData("Hello, Worlt!", "*'Hello, '!'World'", true)]
    [InlineData("Hello, Worlt!", "*'Hello, ''Wor'<1>'t'", true)]
    [InlineData("Hello, Worlt!", "*'Hello, '!'Wer'<1>'t'", true)]
    [InlineData("Hello, Worlds!", "'Hello'*!'s'", true)]
    [InlineData("Hello, Worlds!", "'Hello'*'s!'", true)]
    [InlineData("Hello, Worlds!", "*'ello'*'s!'", true)]
    [InlineData("no you", "*'o y'*", true)]
    public void TestWithPattern(string source, string pattern, bool expected)
    {
        var builder = new MatchContextBuilder()
            .WithFilterString(pattern);
        var searcher = new ContentSearcher();
        var pat = searcher.AddPattern(builder);
        searcher.Build();
        var bytes = Encoding.UTF8.GetBytes(source);
        foreach (var b in bytes)
        {
            if (!searcher.AddByte(b))
            {
                Assert.False(expected);
            }
        }
        Assert.Equal(pat.IsMatch, expected);
    }
}