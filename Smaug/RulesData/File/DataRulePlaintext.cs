using System;
using System.Collections.Generic;
using System.IO;

namespace Smaug.RulesData.File
{
    class DataRulePlaintext : DataRule
    {
        private HashSet<string> AcceptExtensions { get; } = new HashSet<string>()
        {
            ".cf",
            ".cfg",
            ".cnf",
            ".conf",
            ".config",
            ".csv",
            ".env",
            ".ini",
            ".inf",
            ".json",
            ".log",
            ".properties",
            ".sql",
            ".toml",
            ".txt",
            ".yml",
            ".yaml",
            ".xml",
        };

        public override bool? TestRule(string path, byte[] contents, ref List<Tuple<string, string, string>> snippets)
        {
            var extension = Path.GetExtension(path);

            if (!string.IsNullOrEmpty(extension))
            {
                if (AcceptExtensions.Contains(extension.ToLower()))
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
