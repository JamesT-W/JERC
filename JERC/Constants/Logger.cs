using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JERC.Constants
{
    public static class Logger
    {
        public static void LogMessage(string message)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(message);
        }


        public static void LogMessageKey(string message)
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.Write(message);
        }


        public static void LogWarning(string message)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(message);
        }


        public static void LogImportantWarning(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
        }


        public static void LogError(string message)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine(message);
        }


        public static void LogDebugInfo(string message)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine(message);
        }


        public static void LogNewLine()
        {
            Console.WriteLine();
        }
    }
}
