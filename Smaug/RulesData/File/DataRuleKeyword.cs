using System;
using System.Collections.Generic;
using System.IO;

namespace Smaug.RulesData.File
{
    class DataRuleKeyword : DataRule
    {
        public override bool? TestRule(string path, byte[] contents, ref List<string> snippets)
        {
            var result = base.TestRule(path, contents, ref snippets);

            if (result.Value)
                return true;
            else
                return null;
        }

        public override string ToString()
        {
            return "keyword";
        }
    }
}
