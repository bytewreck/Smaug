using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Smaug.RulesData
{
    class DataRule : IDataRule
    {
        public virtual bool? TestRule(string path, byte[] contents, ref List<string> snippets)
        {
            return TestRuleString(path, Encoding.ASCII.GetString(contents), ref snippets, ProgramOptions.SearchKeywords);
        }

        protected bool? TestRule(string path, byte[] contents, ref List<string> snippets, SortedSet<string> keywords)
        {
            return TestRuleString(path, Encoding.ASCII.GetString(contents), ref snippets, keywords);
        }

        protected bool? TestRuleString(string path, string contents, ref List<string> snippets)
        {
            return TestRuleString(path, contents, ref snippets, ProgramOptions.SearchKeywords);
        }

        protected bool? TestRuleString(string path, string contents, ref List<string> snippets, SortedSet<string> keywords)
        {
            foreach (var keyword in keywords)
                GetRegexRanges(contents, keyword, ref snippets);

            return snippets.Count != 0;
        }

        protected void GetRegexRanges(string contents, string pattern, ref List<string> snippets)
        {
            foreach (Match m in Regex.Matches(contents, pattern, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase))
            {
                if (m.Success)
                {
                    var margin = 30;

                    /* Fetch the initial match with margins */
                    var m1 = Math.Min(margin, m.Index);
                    var m2 = Math.Min(margin, contents.Length - (m.Index + m.Length));

                    var i1 = m.Index - m1;
                    var i2 = m.Index + m.Length + m2;

                    var snippet = contents.Substring(i1, i2 - i1);

                    /* Restrict left margins to newlines */
                    if (m1 != 0)
                    {
                        var cr = snippet.LastIndexOf('\r', m1);
                        var lf = snippet.LastIndexOf('\n', m1);

                        if (cr != -1 && lf != -1)
                            snippet = snippet.Substring(Math.Max(cr, lf) + 1);
                        else if (cr != -1 && lf == -1)
                            snippet = snippet.Substring(cr);
                        else if (cr == -1 && lf != -1)
                            snippet = snippet.Substring(lf);
                        else if (i1 != 0)
                            snippet = "..." + snippet;
                    }

                    /* Restrict right margins to newlines */
                    if (m2 != 0)
                    {
                        var cr = snippet.IndexOf("\r", snippet.Length - m2);
                        var lf = snippet.IndexOf("\n", snippet.Length - m2);

                        if (cr != -1 && lf != -1)
                            snippet = snippet.Remove(Math.Min(cr, lf));
                        else if (cr != -1 && lf == -1)
                            snippet = snippet.Remove(cr);
                        else if (cr == -1 && lf != -1)
                            snippet = snippet.Remove(lf);
                        else if (i2 != contents.Length)
                            snippet = snippet + "...";
                    }

                    /* Finalize the snippet */
                    snippets.Add(snippet.Trim());
                }
            }
        }
    }
}
