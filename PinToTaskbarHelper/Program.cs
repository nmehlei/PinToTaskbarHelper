using System;
using PinToTaskbarHelper.Library;

namespace PinToTaskbarHelper
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("PinToTaskbarHelper");
            Console.WriteLine("Created by Nicolas Mehlei, 2018");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();

            if (args.Length == 0)
            {
                Console.WriteLine("Usage: PinToTaskbarHelper.exe PATH");
            }
            else if (args.Length > 1)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Warning: Too many arguments");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Usage: PinToTaskbarHelper.exe PATH");
            }
            else
            {
                Console.Write("Creating application pin, please wait...");

                var path = args[0];
                try
                {
                    TaskbarPinHelper.PinApplication(path);
                    Console.WriteLine("done");
                }
                catch (Exception exc)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("ERROR");
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.Write("StackTrace: ");
                    Console.WriteLine(exc.ToString());
                }
            }
        }
    }
}