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
            if (!string.IsNullOrEmpty(contents))
            {
                foreach (var keyword in keywords)
                    GetRegexRanges(contents, keyword, ref snippets);
            }

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
                        var i3 = snippet.LastIndexOfAny(new char[] { '\0', '\r', '\n' }, m1);

                        if (i3 != -1)
                            snippet = snippet.Substring(i3 + 1);
                        else if (i1 != 0)
                            snippet = "..." + snippet;
                    }

                    /* Restrict right margins to newlines */
                    if (m2 != 0)
                    {
                        var i4 = snippet.IndexOfAny(new char[] { '\0', '\r', '\n' }, snippet.Length - m2);

                        if (i4 != -1)
                            snippet = snippet.Remove(i4);
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
