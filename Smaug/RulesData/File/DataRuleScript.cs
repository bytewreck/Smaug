using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Smaug.RulesData.File
{
    class DataRuleScript : DataRule
    {
        private SortedSet<string> PatternsWindowsCommandCredentials { get; } = new SortedSet<string>()
        {
            "certreq(.exe)?\\s+([^\\r\\n]{0,400}\\s+)?-p\\s",
            "cmdkey(.exe)?\\s+([^\\r\\n]{0,400}\\s+)?/pass:",
            "curl(.exe)?\\s+([^\\r\\n]{0,400}\\s+)?(-u|--user)\\s",
            "driverquery(.exe)?\\s+([^\\r\\n]{0,400}\\s+)?/p\\s",
            "gpresult(.exe)?\\s+([^\\r\\n]{0,400}\\s+)?/p\\s",
            "ksetup(.exe)?\\s+([^\\r\\n]{0,400}\\s+)?/(changepassword|setcomputerpassword)\\s",
            "logman(.exe)?\\s+([^\\r\\n]{0,400}\\s+)?--?u\\s",
            "net(.exe)?\\s+use\\s+([^\\r\\n]{0,400}\\s+)?/user:",
            "net(.exe)?\\s+user\\s",
            "psexec(.exe)?\\s+([^\\r\\n]{0,400}\\s+)?-p\\s",
            "sc(.exe)?\\s+config\\s+([^\\r\\n]{0,400}\\s+)?password=",
            "schtasks(.exe)?\\s+([^\\r\\n]{0,400}\\s+)?(/p\\s|/rp\\s)",
            "sqlcmd(.exe)?\\s+([^\\r\\n]{0,400}\\s+)?-p\\s",
            "(taskkill|tasklist)(.exe)?\\s+([^\\r\\n]{0,400}\\s+)?/p\\s",
        };

        private SortedSet<string> PatternsWindowsPowershellCredentials { get; } = new SortedSet<string>()
        {
            "\\[Net.NetworkCredential\\]::new\\(",
            "ConvertTo\\-SecureString\\s[^\\r\\n]{0,400}\\s\\-AsPlainText",
            "ConvertFrom\\-SecureString\\s[^\\r\\n]{0,400}\\s\\-AsPlainText",
        };

        public override bool? TestRule(string path, byte[] contents, ref List<Tuple<string, string, string>> snippets)
        {
            var extension = Path.GetExtension(path);

            if (!string.IsNullOrEmpty(extension))
            {
                SortedSet<string> patterns = new SortedSet<string>();

                switch (extension.ToLower())
                {
                    /* Bash */
                    case ".sh":
                        break;
                    /* Batch */
                    case ".bat":
                    case ".cmd":
                        patterns.UnionWith(PatternsWindowsCommandCredentials);
                        break;
                    /* PowerShell */
                    case ".ps1":
                    case ".psd1":
                    case ".psm1":
                        patterns.UnionWith(PatternsWindowsCommandCredentials);
                        patterns.UnionWith(PatternsWindowsPowershellCredentials);
                        break;
                    /* VBScript */
                    case ".hta":
                    case ".htm":
                    case ".html":
                        break;
                    case ".vbe":
                    case ".vbs":
                    case ".vbscript":
                    case ".wsc":
                    case ".wsf":
                        break;
                    default:
                        break;
                }

                if (patterns.Count != 0)
                {
                    patterns.UnionWith(ProgramOptions.SearchDataPatterns);
                    return base.TestRule(path, contents, ref snippets, patterns);
                }
            }

            return null;
        }

        public override string ToString()
        {
            return "data:script";
        }
    }
}
