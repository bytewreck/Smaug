using System;
using System.Collections.Generic;

namespace Smaug
{
    class Printer
    {
        public static void MatchRule(string rule_name, string path)
        {
            lock (Console.Out)
            {
                WriteMatch(rule_name, path);
                Console.ResetColor();
            }
        }

        public static void MatchRule(string rule_name, string path, List<Tuple<string, string, string>> snippets)
        {
            lock (Console.Out)
            {
                WriteMatch(rule_name, path);

                foreach (var snippet in snippets)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.Write("\t- " + snippet.Item1);
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.Write(snippet.Item2);
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.WriteLine(snippet.Item3);
                }

                Console.ResetColor();
            }
        }

        private static void WriteMatch(string rule_name, string path)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("[+] Matched rule (");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write(rule_name);
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("): ");
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(path);
        }

        public static void Success(string format, params object[] args)
        {
            WriteLine(ConsoleColor.Green, "[+] " + format, args);
        }

        public static void Error(string format, params object[] args)
        {
            WriteLine(ConsoleColor.Red, "[-] " + format, args);
        }

        public static void Warning(string format, params object[] args)
        {
            WriteLine(ConsoleColor.Yellow, "[!] " + format, args);
        }

        public static void Debug(string format, params object[] args)
        {
            WriteLine(ConsoleColor.Blue, "[#] " + format, args);
        }

        public static void Information(string format, params object[] args)
        {
            WriteLine(ConsoleColor.Gray, "[*] " + format, args);
        }

        private static void WriteLine(ConsoleColor color, string format, params object[] args)
        {
            lock (Console.Out)
            {
                Console.ForegroundColor = color;
                Console.WriteLine(format, args);
                Console.ResetColor();
            }
        }
    }
}
