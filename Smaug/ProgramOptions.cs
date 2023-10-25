using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

using Mono.Options;

namespace Smaug
{
    class ProgramOptions
    {
        /* File criteria */
        public static DateTime AfterDate { get; private set; } = DateTime.MinValue;
        public static DateTime BeforeDate { get; private set; } = DateTime.MaxValue;
        public static long MaxFileSize { get; private set; } = 1024 * 1024;

        /* Search criteria */
        public static SortedSet<string> SearchComputers { get; } = new SortedSet<string>();

#if DEBUG
        public static SortedSet<string> SearchDirectories { get; } = new SortedSet<string>() { "C:\\share_test" };
#else
        public static SortedSet<string> SearchDirectories { get; } = new SortedSet<string>();
#endif

        /* Search keywords */
        public static SortedSet<string> SearchFiletypes { get; } = new SortedSet<string>();
        public static SortedSet<string> SearchKeywords { get; } = new SortedSet<string>();

        /* Performance criteria */
        public static int ThreadCount { get; private set; } = 10;
        public static int Timeout { get; private set; } = 2000;

#if DEBUG
        public static bool Verbose { get; private set; } = true;
#else
        public static bool Verbose { get; private set; } = false;
#endif

        public static bool ParseArgs(string[] args)
        {
            bool show_help = false;

            var options = new OptionSet()
            {
                {
                    "h|help",
                    "Show this message and exit",
                        x => show_help = x != null
                },
                {
                    "a|afterdate=",
                    "Only consider files last modified on or after this date\n\tFormat: dd-MM-yyyy",
                        x =>
                    {
                        if (DateTime.TryParseExact(x, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt))
                            AfterDate = dt;
                        else
                            Printer.Warning("Incorrect DateTime format (--afterdate). Skipping option...");
                    }
                },
                {
                    "b|beforedate=",
                    "Only consider files last modified on or before this date\n\tFormat: dd-MM-yyyy",
                        x =>
                    {
                        if (DateTime.TryParseExact(x, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt))
                            BeforeDate = dt;
                        else
                            Printer.Warning("Incorrect DateTime format (--beforedate). Skipping option...");
                    }
                },
                {
                    "c|computer=",
                    "Include computer in search",
                        x => SearchComputers.Add(x)
                },
                {
                    "d|directory=",
                    "Include directory in search",
                        x => SearchDirectories.Add(x)
                },
                {
                    "f|filetype=",
                    "Include filetype in search",
                        x => SearchFiletypes.Add(x)
                },
                {
                    "k|keyword=",
                    "Include keyword in search (supports regex)",
                        x => SearchKeywords.Add(x)
                },
                {
                    "m|maxsize=",
                    "Maximum file size in bytes (default: 1048576 (1 MiB))\n\tThe .NET framework supports a maximum of MAXINT (2147483647 bytes)",
                        x =>
                    {
                        if (int.TryParse(x, out int mfs))
                            MaxFileSize = mfs;
                        else
                            Printer.Warning("Incorrect long format (--filesize). Skipping option...");
                    }
                },
                {
                    "th|threads=",
                    "Number of concurrent threads (default: 10)",
                        x =>
                    {
                        if (int.TryParse(x, out int tc))
                            ThreadCount = tc;
                        else
                            Printer.Warning("Incorrect int format (--threads). Skipping option...");
                    }
                },
                {
                    "ti|timeout=",
                    "Number of milliseconds to wait for connections (default: 2000)",
                        x =>
                    {
                        if (int.TryParse(x, out int to))
                            Timeout = to;
                        else
                            Printer.Warning("Incorrect int format (--timeout). Skipping option...");
                    }
                },
                {
                    "v|verbose",
                    "Enable verbose output",
                        x => Verbose = x != null
                },
                //{
                //    "<>", x =>
                //    {
                //        Printer.Debug("Default parameter: {0}", x);
                //    }
                //},
            };

            List<string> remainder;

            try
            {
                remainder = options.Parse(args);
            }
            catch (OptionException e)
            {
                Printer.Error("Error: {0}", e.Message);
                Printer.Error("Try 'Smaug.exe --help' for more information.");
                return false;
            }

            if (show_help)
            {
                Console.WriteLine("Usage: Smaug.exe [OPTIONS]+ keywords");
                Console.WriteLine();
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
                return false;
            }

            if (remainder.Count > 0)
            {
                foreach (string r in remainder)
                {
                    Console.WriteLine("Could not map argument(s): {0}", r);
                }
            }

            return true;
        }
    }
}
