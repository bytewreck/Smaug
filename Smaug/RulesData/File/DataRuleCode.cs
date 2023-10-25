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
    class DataRuleCode : DataRule
    {
        public override bool? TestRule(string path, byte[] contents, ref List<string> snippets)
        {
            var temp = Path.GetExtension(path);

            if (temp != null)
            {
                switch (temp.ToLower())
                {
                    case ".bat":
                    case ".cmd":
                        return TestBatch(path, contents, ref snippets);

                    case ".ascx":
                    case ".ashx":
                    case ".asmx":
                    case ".asp":
                    case ".aspx":
                    case ".config":
                    case ".cs":
                    case ".cshtml":
                        return TestCSharp(path, contents, ref snippets);

                    case ".cfm":
                    case ".do":
                    case ".java":
                    case ".jsp":
                        return TestJava(path, contents, ref snippets);

                    case ".js":
                    case ".cjs":
                    case ".mjs":
                    case ".ts":
                    case ".tsx":
                        return TestJavaScript(path, contents, ref snippets);

                    case ".pl":
                        return TestPerl(path, contents, ref snippets);

                    case ".inc":
                    case ".php":
                    case ".php2":
                    case ".php3":
                    case ".php4":
                    case ".php5":
                    case ".php6":
                    case ".php7":
                    case ".phps":
                    case ".pht":
                    case ".phtm":
                    case ".phtml":
                        return TestPhp(path, contents, ref snippets);

                    case ".py":
                        return TestPython(path, contents, ref snippets);

                    case ".ps1":
                    case ".psd1":
                    case ".psm1":
                        return TestPowerShell(path, contents, ref snippets);

                    case ".rb":
                        return TestRuby(path, contents, ref snippets);

                    case ".hta":
                    case ".vbe":
                    case ".vbs":
                    case ".wsc":
                    case ".wsf":
                        return TestVisualBasic(path, contents, ref snippets);

                    default:
                        break;
                }
            }

            return null;
        }

        public override string ToString()
        {
            return "archive";
        }

        private bool? TestBatch(string path, byte[] contents, ref List<string> snippets)
        {
            return null;
        }

        private bool? TestCSharp(string path, byte[] contents, ref List<string> snippets)
        {
            return null;
        }

        private bool? TestJava(string path, byte[] contents, ref List<string> snippets)
        {
            return null;
        }

        private bool? TestJavaScript(string path, byte[] contents, ref List<string> snippets)
        {
            return null;
        }

        private bool? TestPerl(string path, byte[] contents, ref List<string> snippets)
        {
            return null;
        }

        private bool? TestPhp(string path, byte[] contents, ref List<string> snippets)
        {
            return null;
        }

        private bool? TestPowerShell(string path, byte[] contents, ref List<string> snippets)
        {
            return null;
        }

        private bool? TestPython(string path, byte[] contents, ref List<string> snippets)
        {
            return null;
        }

        private bool? TestRuby(string path, byte[] contents, ref List<string> snippets)
        {
            return null;
        }

        private bool? TestVisualBasic(string path, byte[] contents, ref List<string> snippets)
        {
            return null;
        }
    }
}
