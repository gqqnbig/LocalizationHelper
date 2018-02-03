using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Translator
{
    public static class StringExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// Line starts with a non-new-line character, and ends with a new line symbol. 
        /// If input is single line, the return value doesn't have the trailing new line symbol.
        /// </remarks>
        /// <param name="input"></param>
        /// <param name="index">If index is on new line symbol, return the previous line.</param>
        /// <returns>Return value doesn't include \r or \n.</returns>
        public static string GetLineFromIndex(this string input, int index, out int lineIndex)
        {
            string lineEndingSymbol;
            if (input.Contains("\r\n"))
                lineEndingSymbol = "\r\n";
            else if (input.Contains("\r"))
                lineEndingSymbol = "\r";
            else
                lineEndingSymbol = "\n";


            //NOTE: if a match in within startIndex, LastIndexOf will not report the match.
            lineIndex = index - 1 == -1 ? -1 : input.LastIndexOf(lineEndingSymbol, index - 1);
            if (lineIndex == -1)
                lineIndex = 0;
            else
                lineIndex += lineEndingSymbol.Length;

            int p = index - lineEndingSymbol.Length + 1;

            System.Diagnostics.Debug.Assert(p <= index, "As lineEndingSymbol is at least 1 character long, p must be less than or equal to index.");
            System.Diagnostics.Debug.Assert(p < input.Length, "It's the assumption that index is less than input.Length. Therefore p is less than input.Length.");

            if (p < 0)
                p = 0;
            //r is index.
            var r = input.IndexOf(lineEndingSymbol, p);
            if (r == -1)
                r = input.Length;
            else //If match is found, I have to include it because line ending is part of line.
                r += lineEndingSymbol.Length;

            return input.Substring(lineIndex, r - lineIndex);
        }

    }
}
