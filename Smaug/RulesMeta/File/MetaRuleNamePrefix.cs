using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Smaug.RulesMeta.File
{
    class MetaRuleNamePrefix : MetaRule
    {
        private List<string> AcceptNamePrefixes { get; } = new List<string>()
        {

        };

        private List<string> RejectNamePrefixes { get; } = new List<string>()
        {

        };

        public override bool? TestRule(string path)
        {
            var name = Path.GetFileName(path);

            if (!string.IsNullOrEmpty(name))
            {
                var temp = name.ToLower();

                if (RejectNamePrefixes.Any(s => temp.StartsWith(s)))
                    return false;
                else if (AcceptNamePrefixes.Any(s => temp.StartsWith(s)))
                    return true;
            }

            return null;
        }

        public override string ToString()
        {
            return "meta:prefix";
        }
    }
}
