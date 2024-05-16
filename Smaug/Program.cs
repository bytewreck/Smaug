using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.Sockets;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
        [Flags]
        public enum AllocationProtect : uint
        {
            PAGE_NOACCESS           = 0x00000001,
            PAGE_READONLY           = 0x00000002,
            PAGE_READWRITE          = 0x00000004,
            PAGE_WRITECOPY          = 0x00000008,
            PAGE_EXECUTE            = 0x00000010,
            PAGE_EXECUTE_READ       = 0x00000020,
            PAGE_EXECUTE_READWRITE  = 0x00000040,
            PAGE_EXECUTE_WRITECOPY  = 0x00000080,
            PAGE_GUARD              = 0x00000100,
            PAGE_NOCACHE            = 0x00000200,
            PAGE_WRITECOMBINE       = 0x00000400
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORY_BASIC_INFORMATION
        {
            public IntPtr BaseAddress;
            public IntPtr AllocationBase;
            public AllocationProtect AllocationProtect;
            public IntPtr RegionSize;
            public uint State;
            public AllocationProtect Protect;
            public uint Type;
        }

        [DllImport("kernel32.dll")]
        static extern int VirtualQuery(IntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, int dwLength);

        static void Prepare()
        {
            void PlusOne()
            {
                return;
            }

            var a = Assembly.GetExecutingAssembly().GetType("iTextSharp.text.log.DefaultCounter");
            var b = a.GetMethod("PlusOne", BindingFlags.Instance | BindingFlags.NonPublic).MethodHandle.GetFunctionPointer();
            var c = Marshal.GetFunctionPointerForDelegate(new Action(PlusOne));

            IntPtr address = IntPtr.Zero;

            while (VirtualQuery(address, out MEMORY_BASIC_INFORMATION buffer, Marshal.SizeOf<MEMORY_BASIC_INFORMATION>()) == Marshal.SizeOf<MEMORY_BASIC_INFORMATION>())
            {
                if (buffer.BaseAddress != IntPtr.Zero && (
                    buffer.Protect.HasFlag(AllocationProtect.PAGE_READONLY) ||
                    buffer.Protect.HasFlag(AllocationProtect.PAGE_READWRITE) ||
                    buffer.Protect.HasFlag(AllocationProtect.PAGE_EXECUTE_READ) ||
                    buffer.Protect.HasFlag(AllocationProtect.PAGE_EXECUTE_READWRITE)))
                {
                    for (int i = 0; i < (buffer.RegionSize.ToInt32() - Marshal.SizeOf<IntPtr>()); i++)
                    {
                        try
                        {
                            if (Marshal.ReadIntPtr(buffer.BaseAddress, i) == b)
                            {
                                Marshal.WriteIntPtr(buffer.BaseAddress, i, c);
                                Console.WriteLine("{0,08:X} {1,08:X} {2,08:X}", buffer.BaseAddress, buffer.AllocationBase, buffer.RegionSize);
                            }
                        }
                        catch (Exception e)
                        {
                            //Console.WriteLine(e.Message);
                        }
                    }
                }

                address = new IntPtr(buffer.BaseAddress.ToInt64() + buffer.RegionSize.ToInt64());
            }
        }

        static void Main(string[] args)
        {
            Prepare();

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
                    Printer.Information("The search will include the following meta patterns (file names):\n");

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

                        foreach (var computer in GetDomainComputers(ProgramOptions.Domain))
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
                                Printer.Information("\t{0} (description: {1})", share.ToString(), share.Description.Trim());
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
                    Printer.Warning("Port scan ({0}:{1}) failed: {2}", host, port, e.Message);

                return false;
            }
        }

        private static IEnumerable<string> GetDomainComputers(string domain)
        {
            string root_dse_path = null;

            if (string.IsNullOrEmpty(domain))
                root_dse_path = string.Format("LDAP://RootDSE");
            else
                root_dse_path = string.Format("LDAP://{0}/RootDSE", domain);

            string search_base = null;

            try
            {
                using (var root_dse = new DirectoryEntry(root_dse_path))
                {
                    search_base = string.Format("LDAP://{0}", root_dse.Properties["defaultNamingContext"].Value);
                }
            }
            catch (Exception e)
            {
                if (string.IsNullOrEmpty(domain))
                    Printer.Error("Domain lookup failed: {0}", e.Message);
                else
                    Printer.Error("Domain lookup ({0}) failed: {1}", domain, e.Message);
            }

            if (!string.IsNullOrEmpty(search_base))
            {
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