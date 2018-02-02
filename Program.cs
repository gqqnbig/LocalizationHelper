﻿using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace Translator
{
    class Program
    {
        static void Main(string[] args)
        {

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

			if (sourceLanguage == targetLanguage)
				throw new ArgumentException("Source language is the same as target language.");


			TranslateFile(args, sourceFileName, sourceLanguage, targetFileName, targetLanguage);

            //Console.ReadKey();

        }

        private static void TranslateFile(string[] args, string sourceFilePath, string sourceLanguage, string targetFilePath, string targetLanguage)
        {
			if (sourceFilePath == targetFilePath)
				throw new ArgumentException($"source file path is the same as target file path. File path is {sourceFilePath}");

            string keyValuePattern = GetNamedParameter(args, "--keyValuePattern");
			string keyPattern = GetOptionalNamedOptionArgument(args, "--keyPattern", null);

			Regex reg = new Regex(keyPattern!=null? string.Format(keyValuePattern, keyPattern): keyValuePattern, RegexOptions.Multiline);

            var content = File.ReadAllText(sourceFilePath);


            var diffContent = GetDiffContent(args);
			var targetContent = File.Exists(targetFilePath) ? File.ReadAllText(targetFilePath) : null;


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

				var url = $"https://translation.googleapis.com/language/translate/v2/?q={Uri.EscapeDataString(text)}&source={sourceLanguage}&target={targetLanguage}&key={apiToken}";


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
			System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();
			StreamReader sr = null;
            if (diffPath == null)
            {
                if (Console.IsInputRedirected)
					sr = new StreamReader(Console.OpenStandardInput());
			}
			else
				sr = new StreamReader(diffPath);

			if (sr == null)
				return null;

			using (sr)
			{
				//Ignore first 4 lines.
				sr.ReadLine();
				sr.ReadLine();
				sr.ReadLine();
				sr.ReadLine();

				string l;
				while ((l = sr.ReadLine()) != null)
				{
					if (l[0] != '-')
						stringBuilder.AppendLine(l);
				}
			}

			return stringBuilder.ToString();
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

		private static string GetOptionalNamedOptionArgument(string[] args, string option, string defaultValue)
		{
			int p = Array.IndexOf(args, option);
			if (p == -1)
			{
				return null;
			}
			else
			{
				if (p + 1 == args.Length)
					return defaultValue;
				else if (args[p + 1].StartsWith("-"))
					return defaultValue;
				else
					return args[p + 1];
			}
		}
	}
}
