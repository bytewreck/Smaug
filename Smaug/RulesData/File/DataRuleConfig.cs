using System;
using System.Collections.Generic;
using System.IO;

namespace Smaug.RulesData.File
{
    class DataRuleConfig : DataRule
    {
        private HashSet<string> AcceptExtensions { get; } = new HashSet<string>()
        {
            ".cf",
            ".cfg",
            ".cnf",
            ".conf",
            ".config",
            ".env",
            ".ini",
            ".inf",
            ".json",
            ".log",
            ".properties",
            ".toml",
            ".txt",
            ".yml",
            ".yaml",
            ".xml",
        };

        public override bool? TestRule(string path, byte[] contents, ref List<string> snippets)
        {
            var extension = Path.GetExtension(path);

            if (!string.IsNullOrEmpty(extension))
            {
                var temp = extension.ToLower();

                if (AcceptExtensions.Contains(temp))
                    return base.TestRule(path, contents, ref snippets);
            }
            
            return null;
        }

        public override string ToString()
        {
            return "data:config";
        }
    }
}
