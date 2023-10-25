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
            "certreq(.exe)?[^\\r\\n]{0,400}\\s-p\\s",
            "cmdkey(.exe)?\\s[^\\r\\n]{0,400}/pass:",
            "curl(.exe)?[^\\r\\n]{0,400}\\s(-u|--user)\\s",
            "driverquery(.exe)?\\s[^\\r\\n]{0,400}/p\\s",
            "gpresult(.exe)?\\s[^\\r\\n]{0,400}/p\\s",
            "ksetup(.exe)?\\s[^\\r\\n]{0,400}/(changepassword|setcomputerpassword)\\s",
            "logman(.exe)?[^\\r\\n]{0,400}\\s--?u\\s",
            "net(.exe)?\\s+use\\s[^\\r\\n]{0,400}/user:",
            "net(.exe)?\\s+user\\s",
            "psexec(.exe)?[^\\r\\n]{0,400}\\s-p\\s",
            "sc(.exe)?\\s+config[^\\r\\n]{0,400}\\spassword=",
            "schtasks(.exe)?\\s[^\\r\\n]{0,400}(/p\\s|/rp\\s)",
            "sqlcmd(.exe)?[^\\r\\n]{0,400}\\s-p\\s",
            "(taskkill|tasklist)(.exe)?\\s[^\\r\\n]{0,400}/p\\s",
        };

        private SortedSet<string> PatternsWindowsPowershellCredentials { get; } = new SortedSet<string>()
        {
            "\\[Net.NetworkCredential\\]::new\\(",
            "ConvertTo\\-SecureString\\s[^\\r\\n]{0,400}\\s\\-AsPlainText",
            "ConvertFrom\\-SecureString\\s[^\\r\\n]{0,400}\\s\\-AsPlainText",
        };

        public override bool? TestRule(string path, byte[] contents, ref List<string> snippets)
        {
            var temp = Path.GetExtension(path);

            if (temp != null)
            {
                SortedSet<string> patterns = new SortedSet<string>();

                switch (temp.ToLower())
                {
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
                    case ".vbe":
                    case ".vbs":
                    case ".wsc":
                    case ".wsf":
                        break;
                    default:
                        break;
                }

                if (patterns != null && patterns.Count != 0)
                {
                    var result = base.TestRule(path, contents, ref snippets, patterns);

                    if (result.HasValue && result.Value)
                        return true;
                }
            }

            return null;
        }

        public override string ToString()
        {
            return "code";
        }
    }
}
