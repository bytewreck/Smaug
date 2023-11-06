using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Smaug.RulesMeta.File
{
    class MetaRuleRegex : MetaRule
    {
        private HashSet<string> AcceptNames { get; } = new HashSet<string>()
        {

        };

        private HashSet<string> RejectNames { get; } = new HashSet<string>()
        {

        };

        public override bool? TestRule(string path)
        {
            var name = Path.GetFileName(path);

            if (!string.IsNullOrEmpty(name))
            {
                var temp = name.ToLower();

                if (RejectNames.Any(s => Regex.IsMatch(temp, s)))
                    return false;
                else if (AcceptNames.Any(s => Regex.IsMatch(temp, s)) || base.TestDefaultRegex(temp))
                    return true;
            }

            return null;
        }

        public override string ToString()
        {
            return "meta:regex";
        }
    }
}
