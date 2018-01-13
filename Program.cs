using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Translator
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            string rule = @"(?<=(?<k>[\w_]+\.\d+\.[td]:0)\s+"")(?<v>.+)(?="")";

            Regex reg = new Regex(rule);

            var content= File.ReadAllText(args[0]);

            var matches=reg.Matches(content);

            reg.Replace(content, m =>
            {
                Console.WriteLine($"Key: {m.Groups["k"].Value}, Value: {m.Value}");
                return m.Value;
            });

            Console.ReadKey();

        }
    }
}
