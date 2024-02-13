using System;
using System.Collections.Generic;

namespace Smaug.RulesData
{
    interface IDataRule
    {
        bool? TestRule(string path, byte[] contents, ref List<Tuple<string, string, string>> snippets);
    }
}
