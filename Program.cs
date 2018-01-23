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

            string fileNamePattern = GetNamedParameter(args, "--fileNamePattern");


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
			if (sourceFilePath == fileNamePattern)
				throw new ArgumentException($"Parameter fileNamePattern doesn't have format placeholders.\nfileNamePattern={fileNamePattern}");
            string targetFilePath = string.Format(fileNamePattern, targetFileName);

            TranslateFile(args, sourceFilePath, sourceLanguage, targetLanguage, targetFilePath);

            //Console.ReadKey();

        }

        private static void TranslateFile(string[] args, string sourceFilePath, string sourceLanguage, string targetLanguage, string targetFilePath)
        {
			if (sourceFilePath == targetFilePath)
				throw new ArgumentException($"source file path is the same as target file path. File path is {sourceFilePath}");

            string keyValuePattern = GetNamedParameter(args, "--keyValuePattern");
            string keyPattern = GetNamedParameter(args, "--keyPattern");
            Regex reg = new Regex(string.Format(keyValuePattern, keyPattern));

            var content = File.ReadAllText(sourceFilePath);


            var diffContent = GetDiffContent(args);
            var targetContent = File.ReadAllText(targetFilePath);


            var translatedContent = reg.Replace(content, match =>
              {
                  var key = match.Groups["k"].Value;
                  Console.WriteLine($"Key: {key}, Value: {match.Value}");


                  if (diffContent != null)
                  {
                      if (diffContent.Contains(key))
                          return TranslateText(args, sourceLanguage, targetLanguage, match.Value);
                      else
                      {
                          string needle = string.Format(keyValuePattern, key);
                          var m = Regex.Match(targetContent, needle);

                          if (m.Success)
                          {
                              //If the key and value is not changed (not exist in diff), don't have to call translation API.
                              return m.Value;
                          }
                          else
                          {
                              Console.Error.WriteLine($"Key \"{key}\" doesn't exist in diff nor in translated file \"{targetFilePath}\". " +
                                                       "The translated file may be manually modified, the program will translate the value of the key.");

                              return TranslateText(args, sourceLanguage, targetLanguage, match.Value);
                          }
                      }

                  }
                  else
                      return TranslateText(args, sourceLanguage, targetLanguage, match.Value);
              });

            //Console.WriteLine(translatedContent);

            using (StreamWriter sw = new StreamWriter(targetFilePath))
            {
                sw.Write(translatedContent);
            }
        }

        private static string TranslateText(string[] args, string sourceLanguage, string targetLanguage, string text)
        {
            try
            {
                WebClient client = new WebClient();

                string apiToken = GetNamedParameter(args, "--apiToken");
				if (apiToken == null)
					throw new ArgumentNullException("apiToken");

                var url = $"https://translation.googleapis.com/language/translate/v2/?q={text}&source={sourceLanguage}&target={targetLanguage}&key={apiToken}";


                var json = JObject.Parse(client.DownloadString(url));

                var translatedText = json["data"]["translations"][0]["translatedText"];

                return (string)translatedText;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Error in translating: " + e.Message);
                return text;
            }
        }

        private static string GetDiffContent(string[] args)
        {
            string diffPath = GetNamedParameter(args, "--diff");
            string diffContent = null;
            if (diffPath == null)
            {
                if (Console.IsInputRedirected)
                {
                    using (StreamReader sr = new StreamReader(Console.OpenStandardInput()))
                    {
                        //Ignore first 4 lines.
                        sr.ReadLine();
                        sr.ReadLine();
                        sr.ReadLine();
                        sr.ReadLine();

                        diffContent = sr.ReadToEnd();
                    }
                }
            }
            else
            {
                using (StreamReader sr = new StreamReader(diffPath))
                {
                    //Ignore first 4 lines.
                    sr.ReadLine();
                    sr.ReadLine();
                    sr.ReadLine();
                    sr.ReadLine();

                    diffContent = sr.ReadToEnd();
                }
            }

            return diffContent;
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
