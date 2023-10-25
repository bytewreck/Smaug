using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Smaug.RulesMeta.File
{
    class MetaRuleNameSuffix : IMetaRule
    {
        private List<string> AcceptNameSuffixes { get; } = new List<string>()
        {
            /* SSH files */
            "_rsa",
            "_dsa",
            "_ecdsa",
            "_ed25519",
        };

        private List<string> RejectNameSuffixes { get; } = new List<string>()
        {

        };

        public bool? TestRule(string path)
        {
            var name = Path.GetFileName(path);

            if (!string.IsNullOrEmpty(name))
            {
                var temp = name.ToLower();

                if (RejectNameSuffixes.Any(s => temp.EndsWith(s)))
                    return false;
                else if (AcceptNameSuffixes.Any(s => temp.EndsWith(s)))
                    return true;
            }

            return null;
        }

        public override string ToString()
        {
            return "meta:suffix";
        }
    }
}
