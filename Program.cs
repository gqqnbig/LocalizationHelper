using System;
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
			bool isKeylessPattern = keyPattern == null;

			Regex reg = new Regex(isKeylessPattern ? keyValuePattern : string.Format(keyValuePattern, keyPattern), RegexOptions.Multiline);

            var content = File.ReadAllText(sourceFilePath);


			var diffContent = GetDiffContent(args, isKeylessPattern == false);
			var targetContent = File.Exists(targetFilePath) ? File.ReadAllText(targetFilePath) : null;


            var translatedContent = reg.Replace(content, match =>
              {
                  var key = match.Groups["k"].Value;
                  Console.WriteLine($"Key: {key}, Value: {match.Value}");


                  if (diffContent != null)
                  {
                      if (diffContent.Contains(isKeylessPattern ? match.Value : key))
                          return TranslateText(args, sourceLanguage, targetLanguage, match.Value);
                      else if (isKeylessPattern) //Not in diff. I can use the translated verbiage.
                      {
                          var line = content.GetLineFromIndex(match.Index, out int currentLineIndex);

                          var translation = new Func<Regex, string, string, int, string, string>[] { KeylessTranslationProvider, KeylessTranslationLookaheadProvider, KeylessTranslationLookbehindProvider }
                                            .Select(f => f(reg, content, targetContent, currentLineIndex, line)).FirstOrDefault(s => s != null);
                          if (translation != null)
                              return translation;
                          return TranslateText(args, sourceLanguage, targetLanguage, match.Value);
                      }
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
                                                      "I guess you don't want the verbiage translated.");

                              return string.Empty;
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

        private static int GetUniqueKeyInTarget(string targetContent, string key)
        {
            if (key.Trim().Length <= 2) //Ignore very short keys
                return -1;
            else
            {
                var r1 = new Regex("^" + Regex.Escape(key) + ".*", RegexOptions.Multiline);
                var targetMatch = r1.Match(targetContent);
                if (targetMatch.Success == false)
                {
                    Console.Error.WriteLine(
                        $"Key \"{key}\" doesn't exist in diff nor in translated file. I guess this part in target file has been revised.");

                    return -1;
                }
                else if (targetMatch.Index + 1 < targetContent.Length && r1.Match(targetContent, targetMatch.Index + 1).Success)
                {
                    //Key is not unique. Have to translate.
                    return -1;
                }
                else
                {
                    return targetMatch.Index;
                }
            }
        }


        private static string GetInlineKey(Regex reg, string line)
        {
            var m = reg.Match(line);
            string key1;
            if (m.Success)
                key1 = line.Substring(0, m.Index);
            else
                key1 = line;
            return key1;
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

		private static string GetDiffContent(string[] args, bool stripRemovedLines)
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

				if (stripRemovedLines == false)
					return sr.ReadToEnd();

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

        static string KeylessTranslationProvider(Regex reg, string content, string targetContent, int sourceLineIndex, string sourceLine)
        {
            var key1 = GetInlineKey(reg, sourceLine);
            var keyIndex = GetUniqueKeyInTarget(targetContent, key1);
            if (keyIndex == -1)
                return null;

            //Use the value on the same line as the key.
            var l = targetContent.GetLineFromIndex(keyIndex, out _);
            var m = reg.Match(l);
            if (m.Success)
                return m.Value;
            else
            {
                //Key is found, but the line doesn't match KeyValuePattern.
                Console.Error.WriteLine($"Key \"{key1}\" doesn't exist in diff nor in translated file. I guess this verbiage doesn't need to exist.");
                return string.Empty;
            }

        }

        static string KeylessTranslationLookaheadProvider(Regex reg, string content, string targetContent, int sourceLineIndex, string sourceLine)
        {
            var key1 = GetInlineKey(reg, content.GetLineFromIndex(sourceLineIndex + sourceLine.Length, out _));
            var keyIndex = GetUniqueKeyInTarget(targetContent, key1);
            if (keyIndex == -1)
                return null;

            //Because I retrieve the key on next line, I have to get value from its previous line.
            targetContent.GetLineFromIndex(keyIndex, out int p);
            var l = targetContent.GetLineFromIndex(p - 1, out _);
            var m = reg.Match(l);
            if (m.Success)
                return m.Value;
            else
            {
                //Key is found, but the line doesn't match KeyValuePattern.
                Console.Error.WriteLine($"Key \"{key1}\" doesn't exist in diff nor in translated file. I guess this verbiage doesn't need to exist.");
                return string.Empty;
            }
        }

        static string KeylessTranslationLookbehindProvider(Regex reg, string content, string targetContent, int sourceLineIndex, string sourceLine)
        {
            var key1 = GetInlineKey(reg, content.GetLineFromIndex(sourceLineIndex - 1, out _));
            var keyIndex = GetUniqueKeyInTarget(targetContent, key1);
            if (keyIndex == -1)
                return null;


            //Because I retrieve the key on previous line, I have to get value from its next line.
            var l0 = targetContent.GetLineFromIndex(keyIndex, out int p);
            var l = targetContent.GetLineFromIndex(p + l0.Length, out _);
            var m = reg.Match(l);
            if (m.Success)
                return m.Value;
            else
            {
                //Key is found, but the line doesn't match KeyValuePattern.
                Console.Error.WriteLine($"Key \"{key1}\" doesn't exist in diff nor in translated file. I guess this verbiage doesn't need to exist.");
                return string.Empty;
            }
        }
	}
}
