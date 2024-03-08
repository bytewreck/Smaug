using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.Sockets;
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
            sw.Start();

            if (ProgramOptions.ParseArgs(args))
            {
                if (ProgramOptions.SearchMetaPatterns.Count == 0 && ProgramOptions.SearchDataPatterns.Count == 0)
                {
                    Printer.Information("No patterns specified. Restoring default patterns...\n");
                    ProgramOptions.SearchMetaPatterns.UnionWith(ProgramOptions.SearchMetaDefaultPatterns);
                    ProgramOptions.SearchDataPatterns.UnionWith(ProgramOptions.SearchDataDefaultPatterns);
                }

                if (ProgramOptions.SearchMetaPatterns.Count != 0)
                {
                    Printer.Information("The search will include the following meta patterns (file name):\n");

                    foreach (var pattern in ProgramOptions.SearchMetaPatterns)
                        Printer.Information("\t{0}", pattern);

                    Printer.Information("");
                }

                if (ProgramOptions.SearchDataPatterns.Count != 0)
                {
                    Printer.Information("The search will include the following data patterns (file contents):\n");

                    foreach (var pattern in ProgramOptions.SearchDataPatterns)
                        Printer.Information("\t{0}", pattern);

                    Printer.Information("");
                }

                var computers = ProgramOptions.SearchComputers;
                var directories = ProgramOptions.SearchDirectories;

                if (computers.Count == 0 && directories.Count == 0)
                {
                    Printer.Information("No targets specified. Finding targets automatically...\n");

                    if (IsPartOfDomain())
                    {
                        Printer.Information("Enumerating domain computers...\n");

                        foreach (var computer in GetDomainComputers())
                        {
                            Printer.Information("\t{0}", computer);
                            computers.Add(computer);
                        }

                        Printer.Information("\nIdentified {0} domain computer(s).\n", computers.Count);
                    }
                    else
                    {
                        Printer.Information("Enumerating local drives...\n");

                        foreach (var drive in GetLocalDrives())
                        {
                            Printer.Information("\t{0}", drive);
                            directories.Add(drive);
                        }

                        Printer.Information("\nIdentified {0} local drive(s).\n", directories.Count);
                    }
                }

                if (computers.Count != 0)
                {
                    Printer.Information("Enumerating shares on {0} domain computer(s).\n", computers.Count);

                    var shares = new ConcurrentBag<string>();

                    Parallel.ForEach(computers, new ParallelOptions() { MaxDegreeOfParallelism = ProgramOptions.ThreadCount }, hostname =>
                    {
                        if (IsPortOpen(hostname, 445) || IsPortOpen(hostname, 139))
                        {
                            foreach (var share in NetworkShares.EnumerateShares(hostname))
                            {
                                Printer.Information("\t{0} (description: {1})", share.ToString(), share.Description);
                                shares.Add(share.ToString());
                            }
                        }
                    });

                    Printer.Information("\nIdentified {0} network share(s).\n", shares.Count);

                    directories.UnionWith(shares.ToList());
                }

                if (directories.Count == 0)
                    Printer.Error("Could not identify any targets.");
                else
                {
                    Printer.Information("Launching scan against {0} target(s).\n", directories.Count);

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

            sw.Stop();

            Console.WriteLine("Done. Time elapsed: {0}", sw.Elapsed);

            if (Debugger.IsAttached)
                Console.ReadKey();
        }

        private static bool IsPartOfDomain()
        {
            using (var cs = new ManagementObject(string.Format("Win32_ComputerSystem.Name='{0}'", Environment.MachineName)))
            {
                cs.Get();

                var pod = cs["PartOfDomain"];
                return (pod != null && (bool)pod != false);
            }
        }

        private static bool IsPortOpen(string host, int port)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    var task = client.ConnectAsync(host, port);
                    return (task.Wait(ProgramOptions.Timeout) && client.Connected);
                }
            }
            catch (Exception e)
            {
                if (ProgramOptions.Verbose)
                    Printer.Warning(e.Message);

                return false;
            }
        }

        private static IEnumerable<string> GetDomainComputers()
        {
            using (var root_dse = new DirectoryEntry("LDAP://RootDSE"))
            {
                var search_base = string.Format("LDAP://{0}", root_dse.Properties["defaultNamingContext"].Value);

                using (var dir_entry = new DirectoryEntry(search_base))
                {
                    using (var dir_search = new DirectorySearcher(dir_entry, "(&(objectClass=computer)(dNSHostName=*))"))
                    {
                        dir_search.PageSize = 1000;

                        using (var dir_results = dir_search.FindAll())
                        {
                            foreach (SearchResult dir_result in dir_results)
                                yield return dir_result.Properties["dNSHostName"][0].ToString();
                        }
                    }
                }
            }
        }

        private static IEnumerable<string> GetLocalDrives()
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady && drive.DriveType == DriveType.Fixed)
                    yield return drive.Name;
            }
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
#if DEBUG
                    else if (ProgramOptions.Verbose)
                        Printer.Debug("Rejecting ({0}): {1}", rule, path);
#endif

                    return true;
                }
            }

            return false;
        }

        private static List<IDataRule> FileContentRules { get; } = new List<IDataRule>()
        {
            new DataRuleCode(),
            new DataRuleOffice(),
            new DataRuleScript(),
            new DataRulePlaintext(),
        };

        static private bool IsMatchContentRules(FileStream fs, string path, List<IDataRule> rules)
        {
            var contents = new byte[fs.Length];

            for (int offset = 0, read = 0; offset < (int)fs.Length; offset += read)
                read = fs.Read(contents, offset, (int)fs.Length - offset);

            var snippets = new List<Tuple<string, string, string>>();

            foreach (var rule in rules)
            {
                var result = rule.TestRule(path, contents, ref snippets);

                if (result.HasValue)
                {
                    if (result.Value)
                        Printer.MatchRule(rule.ToString(), path, snippets);
#if DEBUG
                    else if (ProgramOptions.Verbose)
                        Printer.Debug("Rejecting ({0}): {1}", rule, path);
#endif

                    return true;
                }
            }

            return false;
        }
    }
}