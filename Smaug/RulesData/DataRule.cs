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
            return TestRuleString(path, Encoding.ASCII.GetString(contents), ref snippets, ProgramOptions.SearchPatterns);
        }

        protected bool? TestRule(string path, byte[] contents, ref List<string> snippets, SortedSet<string> keywords)
        {
            return TestRuleString(path, Encoding.ASCII.GetString(contents), ref snippets, keywords);
        }

        protected bool? TestRuleString(string path, string contents, ref List<string> snippets)
        {
            return TestRuleString(path, contents, ref snippets, ProgramOptions.SearchPatterns);
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

        private static char[] BoundaryCharacters { get; } = new char[]
        {
            (char)0x00, (char)0x01, (char)0x02, (char)0x03, (char)0x04, (char)0x05, (char)0x06, (char)0x07,
            (char)0x08, (char)0x09, (char)0x0a, (char)0x0b, (char)0x0c, (char)0x0d, (char)0x0e, (char)0x0f,
            (char)0x10, (char)0x11, (char)0x12, (char)0x13, (char)0x14, (char)0x15, (char)0x16, (char)0x17,
            (char)0x18, (char)0x19, (char)0x1a, (char)0x1b, (char)0x1c, (char)0x1d, (char)0x1e, (char)0x1f,
        };

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
                        var i3 = snippet.LastIndexOfAny(BoundaryCharacters, m1);

                        if (i3 != -1)
                            snippet = snippet.Substring(i3 + 1);
                        else if (i1 != 0)
                            snippet = "..." + snippet;
                    }

                    /* Restrict right margins to newlines */
                    if (m2 != 0)
                    {
                        var i4 = snippet.IndexOfAny(BoundaryCharacters, snippet.Length - m2);

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
