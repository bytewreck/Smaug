using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

using Mono.Options;

namespace Smaug
{
    class ProgramOptions
    {
        public static DateTime AfterDate { get; private set; } = DateTime.MinValue;
        public static DateTime BeforeDate { get; private set; } = DateTime.MaxValue;
        public static long MaxFileSize { get; private set; } = 1024 * 1024;

        public static SortedSet<string> SearchComputers { get; } = new SortedSet<string>();
        public static SortedSet<string> SearchDirectories { get; } = new SortedSet<string>();

        public static SortedSet<string> SearchDataPatterns { get; } = new SortedSet<string>();
        public static SortedSet<string> SearchMetaPatterns { get; } = new SortedSet<string>();

        public static SortedSet<string> SearchDataDefaultPatterns { get; } = new SortedSet<string>()
        {
            "CREATE\\s+USER\\s+(IF\\s+NOT\\s+EXISTS\\s+)?[^\\r\\n]{0,32}\\s+IDENTIFIED\\s+BY",
            "CREATE\\s+LOGIN\\s+[^\\r\\n]{0,256}\\s+WITH\\s+PASSWORD",
            "-----BEGIN\\s+([^\\r\\n]{0,100}\\s+)?PRIVATE\\s+KEY(\\s+BLOCK)?-----",
            //"((\"|')?AWS_ACCESS_KEY_ID(\"|')?\\s*(:|=>|=)\\s*)?(\"|')?AKIA[\\w]{16}(\"|')?",
            "(\"|')?AWS_SECRET_ACCESS_KEY(\"|')?\\s*(:|=>|=)\\s*(\"|')?[\\w+/=]{40}(\"|')?",
            "s3://[a-z0-9\\.\\-]{3,64}/?",
            "pass(word|wrd|wd|w)(\\s*=\\s*(\"|')?)?",
            "client_secret(\\s*=\\s*(\"|')?)?",
            //"secret(\\s*=\\s*(\"|')?)?",
            "(api|aws|private)[_\\-\\.\\s]?key(\\s*=\\s*(\"|')?)?",
        };

        public static SortedSet<string> SearchMetaDefaultPatterns { get; } = new SortedSet<string>()
        {
            "pass(word|wrd|wd|w)?",
            "secret",
            "(api|aws|private)[_\\-\\.\\s]?key",
        };

        public static int ThreadCount { get; private set; } = 10;
        public static int Timeout { get; private set; } = 2000;

        public static bool Verbose { get; set; } = false;

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
                    "Only consider files last modified >= this date\n\tFormat: dd-MM-yyyy",
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
                    "Only consider files last modified <= this date\n\tFormat: dd-MM-yyyy",
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
                    "k|regex-content=",
                    "Add pattern to file content search",
                        x => SearchDataPatterns.Add(x)
                },
                {
                    "default-content",
                    "Include default patterns in file content search",
                        x => SearchDataPatterns.UnionWith(SearchDataDefaultPatterns)
                },
                {
                    "n|regex-name=",
                    "Add pattern to file name search",
                        x => SearchMetaPatterns.Add(x)
                },
                {
                    "default-name",
                    "Include default patterns in file name search",
                        x => SearchMetaPatterns.UnionWith(SearchMetaDefaultPatterns)
                },
                {
                    "m|maxsize=",
                    "Maximum file size in bytes\n\tDefault: 1048576",
                        x =>
                    {
                        if (int.TryParse(x, out int mfs))
                            MaxFileSize = mfs;
                        else
                            Printer.Warning("Incorrect long format (--filesize). Skipping option...");
                    }
                },
                {
                    "threads=",
                    "Number of concurrent threads\n\tDefault: 10",
                        x =>
                    {
                        if (int.TryParse(x, out int tc))
                            ThreadCount = tc;
                        else
                            Printer.Warning("Incorrect int format (--threads). Skipping option...");
                    }
                },
                {
                    "timeout=",
                    "Number of milliseconds to wait for connections\n\tDefault: 2000",
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
