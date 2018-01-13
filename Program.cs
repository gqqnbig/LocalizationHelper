using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace Translator
{
    class Program
    {
        static void Main(string[] args)
        {
            string keyValueRule = @"(?<=(?<k>[\w_]+\.\d+\.[td]:0)\s+"")(?<v>.+)(?="")";

            string fileNamePattern = GetNamedParameter(args, "--fileNamePattern");// ".+?_l_{0}.yml";

            string apiToken = GetNamedParameter(args, "--apiToken");

            string source = GetNamedParameter(args, "--source");
            var parts = source.Split(':');
            if (parts.Length != 2)
                throw new ArgumentException("The value of source must take form of \"fileName:LanguageName\", where LanguageName is the support language in the translator API, eg. eng:en or whatever:zh");
            string sourceFileName = parts[0];
            string sourceLanguage = parts[1];

            string target = GetNamedParameter(args, "--target");
            parts = target.Split(':');
            if (parts.Length != 2)
                throw new ArgumentException("The value of target must take form of \"fileName:LanguageName\", where LanguageName is the support language in the translator API, eg. eng:en or whatever:zh");
            string targetFileName = parts[0];
            string targetLanguage = parts[1];



            string sourceFilePath = string.Format(fileNamePattern, sourceFileName);
            Regex reg = new Regex(keyValueRule);

            var content = File.ReadAllText(sourceFilePath);

            WebClient client = new WebClient();
            var translatedContent = reg.Replace(content, m =>
              {
                  //Console.WriteLine($"Key: {m.Groups["k"].Value}, Value: {m.Value}");

                  try
                  {
                      var url =$"https://translation.googleapis.com/language/translate/v2/?q={m.Value}&source={sourceLanguage}&target={targetLanguage}&key={apiToken}";



                      var json = JObject.Parse(client.DownloadString(url));

                      var translatedText = json["data"]["translations"][0]["translatedText"];

                      return (string) translatedText;
                  }
                  catch (Exception e)
                  {
                      Console.Error.WriteLine("Error in translating: " + e.Message);
                      return m.Value;
                  }
              });

            //Console.WriteLine("Translation finished");

            //Console.WriteLine();
            Console.WriteLine(translatedContent);

            //Console.ReadKey();

        }

        static string GetNamedParameter(string[] args, string name, string shortName = null)
        {
            int p = Array.IndexOf(args, name);
            if (p == -1)
            {
                if (shortName != null)
                    return GetNamedParameter(args, shortName);
                else
                    return null;
            }

            if (p == args.Length)
                throw new ArgumentException($"Option {name} requires a value");

            return args[p + 1];
        }
    }
}
