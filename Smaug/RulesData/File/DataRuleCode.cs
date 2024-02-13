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
        private SortedSet<string> PatternsCppDatabaseConnection { get; } = new SortedSet<string>()
        {
            "mysql_connect\\s*\\(",
            "mysql_real_connect\\s*\\(",
            "mysql_real_connect_dns_srv\\s*\\(",
            "mysql_real_connect_nonblocking\\s*\\(",
            "SQLConfigDataSource",
            "SQLDriverConnect",
            "SQLSetConnectOption",
        };

        private SortedSet<string> PatternsDotNetDatabaseConnection { get; } = new SortedSet<string>()
        {
            "(Data Source|Server|Address|Addr|Network Address)\\s*=[^\\r\\n]+;\\s*(Password|PWD)\\s*=",
            "(Password|PWD)\\s*=[^\\r\\n]+;\\s*(Data Source|Server|Address|Addr|Network Address)\\s*=",
            "(Data Source|Server|Address|Addr|Network Address)\\s*=[^\\r\\n]+;\\s*(Integrated Security|Trusted_Connection)\\s*=\\s*(SSPI|true|yes);",
            "(Integrated Security|Trusted_Connection)\\s*=\\s*(SSPI|true|yes)[^\\r\\n]+;\\s*(Data Source|Server|Address|Addr|Network Address)\\s*=",
        };

        private SortedSet<string> PatternsJavaDatabaseConnection { get; } = new SortedSet<string>()
        {
           "DriverManager\\.getConnection\\(",
            "\"jdbc:",
        };

        private SortedSet<string> PatternsKotlinDatabaseConnection { get; } = new SortedSet<string>()
        {
            "=jdbc:",
        };

        private SortedSet<string> PatternsPerlDatabaseConnection { get; } = new SortedSet<string>()
        {
            "DBI->connect\\(",
        };

        private SortedSet<string> PatternsPhpDatabaseConnection { get; } = new SortedSet<string>()
        {
            "new\\s+PDO\\s*\\(",
            "mysqli_connect\\s*\\(",
            "mysqli_real_connect\\s*\\(",
            "mysqli_change_user\\s*\\(",
            "->change_user\\s*\\(",
            "new\\s+mysqli\\s*\\(",
            "->connect\\s*\\(",
            "mysqli?_connect\\s*\\(",
            "mysql_pconnect\\s*\\(",
            "mysqli?_change_user\\s*\\(",
            "pg_connect\\s*\\(",
            "pg_pconnect\\s*\\(",
        };

        private SortedSet<string> PatternsPythonDatabaseConnection { get; } = new SortedSet<string>()
        {
            "mysql\\.connector\\.connect\\(",
            "pyodbc\\.connect\\(",
            "psycopg2\\.connect\\(",
        };

        private SortedSet<string> PatternsRubyDatabaseConnection { get; } = new SortedSet<string>()
        {
            "DBI\\.connect\\(",
        };

        private SortedSet<string> PatternsGolangDatabaseConnection { get; } = new SortedSet<string>()
        {
            "sql\\.Open\\(\"",
        };

        private SortedSet<string> PatternsRustDatabaseConnection { get; } = new SortedSet<string>()
        {
            "mysql://",
            "postgresql://",
        };

        private SortedSet<string> PatternsVisualBasicDatabaseConnection { get; } = new SortedSet<string>()
        {
            "New\\s+sqlConnection\\(\"",
        };
        
        public override bool? TestRule(string path, byte[] contents, ref List<Tuple<string, string, string>> snippets)
        {
            var extension = Path.GetExtension(path);

            if (!string.IsNullOrEmpty(extension))
            {
                SortedSet<string> patterns = new SortedSet<string>();

                switch (extension.ToLower())
                {
                    /* C / C++ */
                    case ".c":
                    case ".cc":
                    case ".cpp":
                    case ".h":
                    case ".hh":
                    case ".hpp":
                        patterns.UnionWith(PatternsCppDatabaseConnection);
                        break;
                    /* .NET (C# / ASP) */
                    case ".ascx": 
                    case ".ashx":
                    case ".asmx":
                    case ".asp":
                    case ".aspx":
                    case ".config":
                    case ".cs":
                    case ".cshtml":
                        patterns.UnionWith(PatternsDotNetDatabaseConnection);
                        break;
                    /* Golang */
                    case ".go":
                        patterns.UnionWith(PatternsGolangDatabaseConnection);
                        break;
                    /* Java */
                    case ".cfm":
                    case ".do":
                    case ".java":
                    case ".jsp":
                        patterns.UnionWith(PatternsJavaDatabaseConnection);
                        break;
                    /* JavaScript / TypeScript */
                    case ".js":
                    case ".cjs":
                    case ".mjs":
                    case ".ts":
                    case ".tsx":
                        break;
                    /* Kotlin */
                    case ".kt":
                    case ".kts":
                        patterns.UnionWith(PatternsJavaDatabaseConnection);
                        patterns.UnionWith(PatternsKotlinDatabaseConnection);
                        break;
                    /* Perl */
                    case ".pl":
                        patterns.UnionWith(PatternsPerlDatabaseConnection);
                        break;
                    /* PHP */
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
                        patterns.UnionWith(PatternsPhpDatabaseConnection);
                        break;
                    /* Python */
                    case ".py":
                        patterns.UnionWith(PatternsPythonDatabaseConnection);
                        break;
                    /* Ruby */
                    case ".rb":
                        patterns.UnionWith(PatternsRubyDatabaseConnection);
                        break;
                    /* Rust */
                    case ".rs":
                        patterns.UnionWith(PatternsRustDatabaseConnection);
                        break;
                    /* Visual Basic */
                    case ".vb":
                        patterns.UnionWith(PatternsVisualBasicDatabaseConnection);
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
            return "data:code";
        }
    }
}
