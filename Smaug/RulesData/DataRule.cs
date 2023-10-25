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
            return TestRuleString(path, Encoding.ASCII.GetString(contents), ref snippets);
        }

        protected virtual bool? TestRuleString(string path, string contents, ref List<string> snippets)
        {
            foreach (var keyword in ProgramOptions.SearchKeywords)
                GetRegexRanges(contents, keyword, ref snippets);

            return snippets.Count != 0;
        }

        private void GetRegexRanges(string contents, string pattern, ref List<string> snippets)
        {
            foreach (Match m in Regex.Matches(contents, pattern))
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

                    /* Restrict margins to newlines */
                    var i3 = snippet.LastIndexOf('\n', m1);

                    if (i3 != -1)
                        snippet = snippet.Substring(i3 + 1);
                    else if (i1 != 0)
                        snippet = "..." + snippet;

                    var i4 = snippet.IndexOf("\n", snippet.Length - m2);

                    if (i4 != -1)
                        snippet = snippet.Remove(i4);
                    else if (i2 != contents.Length)
                        snippet = snippet + "...";

                    snippets.Add(snippet.Trim());
                }
            }
        }
    }
}
