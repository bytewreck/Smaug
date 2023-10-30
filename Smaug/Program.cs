using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Smaug.RulesData;
using Smaug.RulesData.File;
using Smaug.RulesMeta;
using Smaug.RulesMeta.Directory;
using Smaug.RulesMeta.File;

namespace Smaug
{
    class Program
    {
        static void Main(string[] args)
        {
            var sw = new Stopwatch();

            if (ProgramOptions.ParseArgs(args))
            {
                if (ProgramOptions.SearchPatterns.Count == 0)
                {
                    Printer.Information("No keywords specified. Restoring default keywords.");
                    ProgramOptions.SearchPatterns.Add("CREATE\\s+USER\\s+(IF\\s+NOT\\s+EXISTS\\s+)?[^\\r\\n]{0,32}\\s+IDENTIFIED\\s+BY");
                    ProgramOptions.SearchPatterns.Add("CREATE\\s+LOGIN\\s+[^\\r\\n]{0,256}\\s+WITH\\s+PASSWORD");
                    ProgramOptions.SearchPatterns.Add("-----BEGIN\\s+([^\\r\\n]{0,100}\\s+)?PRIVATE\\s+KEY(\\s+BLOCK)?-----");
                    ProgramOptions.SearchPatterns.Add("((\"|')?AWS_ACCESS_KEY_ID(\"|')?\\s*(:|=>|=)\\s*)?(\"|')?AKIA[\\w]{16}(\"|')?");
                    ProgramOptions.SearchPatterns.Add("(\"|')?AWS_SECRET_ACCESS_KEY(\"|')?\\s*(:|=>|=)\\s*(\"|')?[\\w+/=]{40}(\"|')?");
                    ProgramOptions.SearchPatterns.Add("s3://[a-z0-9\\.\\-]{3,64}/?");
                    ProgramOptions.SearchPatterns.Add("pass(word|wrd|wd|w)(\\s*=\\s*(\"|')?)?");
                    ProgramOptions.SearchPatterns.Add("<pass(word|wrd|wd|w)>[^\\r\\n]+</pass(word|wrd|wd|w)>");
                    ProgramOptions.SearchPatterns.Add("secret(\\s*=\\s*(\"|')?)?");
                    ProgramOptions.SearchPatterns.Add("(api|aws|private)[_\\-\\.\\s]?key(\\s*=\\s*(\"|')?)?");
                    ProgramOptions.SearchPatterns.Add("client_secret(\\s*=\\s*(\"|')?)?");
                }

                var computers = ProgramOptions.SearchComputers;
                var directories = ProgramOptions.SearchDirectories;

                if (computers.Count == 0 && directories.Count == 0)
                {
                    Printer.Information("No targets specified. Enumerating all domain computers...");
                    computers.UnionWith(LdapSearcher.GetDomainComputers());
                    Printer.Information("Identified {0} domain computer(s)", computers.Count);
                }

                foreach (var share in NetworkShares.EnumerateShares(computers))
                    directories.Add(share.ToString());

                if (directories.Count == 0)
                    Printer.Error("Could not identify any targets");
                else
                {
                    Printer.Information("Targeting {0} total directories", directories.Count);

                    int index = 0;
                    int length = directories.Count;

                    Parallel.ForEach(directories, new ParallelOptions() { MaxDegreeOfParallelism = ProgramOptions.ThreadCount }, directory =>
                    {
                        if (ProgramOptions.Verbose)
                            Printer.Debug("[{0}/{1}] Scanning directory: {2}", Interlocked.Increment(ref index), length, directory);

                        TraverseDirectory(directory);
                    });
                }
            }

            Console.WriteLine("Done. Time elapsed: {0}", sw.Elapsed);

            if (Debugger.IsAttached)
                Console.ReadKey();
        }

        private static void TraverseDirectory(string path)
        {
            try
            {
                var info = new DirectoryInfo(path);

                if (info.Exists &&
                    info.LastWriteTime >= ProgramOptions.AfterDate &&
                    info.LastWriteTime <= ProgramOptions.BeforeDate)
                {
                    if (info.EnumerateFiles().FirstOrDefault() != null ||
                        info.EnumerateDirectories().FirstOrDefault() != null)
                    {
                        if (!IsMatchMetaRules(path, DirectoryMetaRules))
                        {
                            foreach (var directory in Directory.EnumerateDirectories(path))
                                TraverseDirectory(directory);

                            foreach (var file in Directory.EnumerateFiles(path))
                                TraverseFile(file.Replace("{", "{{").Replace("}", "}}"));
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                if (ProgramOptions.Verbose)
                    Printer.Debug("Rejecting (directory:noaccess): {0}", path);
            }
            catch (Exception e)
            {
                if (ProgramOptions.Verbose)
                    Printer.Warning("Exception for '{0}' - {1}", path, e.ToString());
            }
        }

        private static bool TraverseFile(string path)
        {
            try
            {
                var info = new FileInfo(path);

                if (info.Exists &&
                    info.Length <= ProgramOptions.MaxFileSize &&
                    info.LastWriteTime >= ProgramOptions.AfterDate &&
                    info.LastWriteTime <= ProgramOptions.BeforeDate)
                {
                    using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                    {
                        if (fs.CanRead && !IsMatchMetaRules(path, FileMetaRules) &&
                            fs.Length != 0 && !IsMatchContentRules(fs, path, FileContentRules))
                        {
                            if (ProgramOptions.Verbose)
                                Printer.Debug("No match: {0}", path);
                        }
                    }
                }
            }
            catch (FileFormatException)
            {
                if (ProgramOptions.Verbose)
                    Printer.Debug("Rejecting (file:corrupt): {0}", path);
            }
            catch (UnauthorizedAccessException)
            {
                if (ProgramOptions.Verbose)
                    Printer.Debug("Rejecting (file:noaccess): {0}", path);
            }
            catch (Exception e)
            {
                if (ProgramOptions.Verbose)
                    Printer.Warning("Exception for '{0}' - {1}", path, e.ToString());
            }

            return false;
        }

        private static List<IMetaRule> DirectoryMetaRules { get; } = new List<IMetaRule>()
        {
            new MetaRulePath(),
        };

        private static List<IMetaRule> FileMetaRules { get; } = new List<IMetaRule>()
        {
            new MetaRuleName(),
            new MetaRuleNamePrefix(),
            new MetaRuleNameSuffix(),
            new MetaRuleRegex(),
            new MetaRuleExtension(),
        };

        static private bool IsMatchMetaRules(string path, List<IMetaRule> rules)
        {
            foreach (var rule in rules)
            {
                var result = rule.TestRule(path);
                
                if (result.HasValue)
                {
                    if (result.Value)
                        Printer.MatchRule(rule.ToString(), path);
                    else if (ProgramOptions.Verbose)
                        Printer.Debug("Rejecting ({0}): {1}", rule, path);

                    return true;
                }
            }

            return false;
        }

        private static List<IDataRule> FileContentRules { get; } = new List<IDataRule>()
        {
            new DataRuleCode(),
            new DataRuleConfig(),
            new DataRuleOffice(),
            new DataRuleScript(),
        };

        static private bool IsMatchContentRules(FileStream fs, string path, List<IDataRule> rules)
        {
            var contents = new byte[fs.Length];

            for (int offset = 0, read = 0; offset < (int)fs.Length; offset += read)
                read = fs.Read(contents, offset, (int)fs.Length - offset);

            var snippets = new List<string>();

            foreach (var rule in rules)
            {
                var result = rule.TestRule(path, contents, ref snippets);

                if (result.HasValue)
                {
                    if (result.Value)
                        Printer.MatchRule(rule.ToString(), path, snippets);
                    else if (ProgramOptions.Verbose)
                        Printer.Debug("Rejecting ({0}): {1}", rule, path);

                    return true;
                }
            }

            return false;
        }
    }
}