using System.Text;
using Neutrino.ContentSearch.Filters;

namespace Neutrino.ContentSearch;

public class PatternParser
{
    public static IReadOnlyList<ContentFilter> Parse(string val, Encoding encoding)
    {
        var res = TryParse(val, out var l, encoding);
        if (!res) throw new InvalidOperationException("The specified string is not a valid filter");
        return l;
    }

    public static bool TryParse(string val, out IReadOnlyList<ContentFilter> filter, Encoding encoding)
    {
        var v = new List<ContentFilter>();
        filter = v;
        bool isEscaped = false, isInText = false, isInNegation = false;
        var curText = new StringBuilder();
        for (int i = 0; i < val.Length; i++)
        {
            if (val[i] == '*' && !isInText)
            {
                if (isEscaped || isInNegation) return false;
                v.Add(new AnyFilter());
            }
            else if (val[i] == '!' && !isInText)
            {
                if (isEscaped || isInNegation) return false;
                isInNegation = true;
            }
            else if (val[i] == '\'' && !isEscaped)
            {
                if (isInText)
                {
                    if (curText.Length is not 0)
                    {
                        if (isInNegation)
                        {
                            v.Add(new NotEqualFilter(encoding.GetBytes(curText.ToString())));
                        }
                        else
                        {
                            v.Add(new EqualFilter(encoding.GetBytes(curText.ToString())));
                        }
                        curText.Clear();
                    }

                    isInText = false;
                    isInNegation = false;
                }
                else
                {
                    isInText = true;
                }
            }
            else if (val[i] == '\\')
            {
                if(!isInText) return false;
                if (isEscaped)
                {
                    isEscaped = false;
                    curText.Append(val[i]);
                }
                else
                {
                    isEscaped = true;
                }
            }
            else if (val[i] == '<' && !isInText)
            {
                int k = i;
                for (; k < val.Length; k++)
                {
                    if (val[k] == '>')
                    {
                        break;
                    }
                }

                if (i + 1 > k) return false;
                var success = long.TryParse(val[(i+1)..k], out var result);
                if (!success) return false;
                v.Add(new AnyFixedFilter(result));
                i = k;
            }
            else if (isInText)
            {
                if (isEscaped)
                {
                    if (val[i] is not '\'' and not '\\')
                    {
                        return false;
                    }

                    isEscaped = false;
                }
                curText.Append(val[i]);
            }
            else return false;
        }

        return true;
    }
}