// <copyright file="StringExtensionsTest.cs">Copyright ©  2018</copyright>

using System;
using Microsoft.Pex.Framework;
using Microsoft.Pex.Framework.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Translator;
using System.Linq;

// ReSharper disable InvokeAsExtensionMethod

namespace Translator.Tests
{
    /// <summary>此类包含 StringExtensions 的参数化单元测试</summary>
    [PexClass(typeof(StringExtensions))]
    [TestClass]
    public partial class StringExtensionsTest
    {

        [PexMethod]
        public string GetLineFromIndex(
            string content,
            int index,
            out int lineIndex
        )
        {
            PexAssume.IsNotNull(content);
            int countCR= content.Count(c => c=='\r');
            int countLN = content.Count(c => c == '\n');
            PexAssume.IsTrue(countCR == countLN || countCR == 0 || countLN == 0, "Mixed line ending is not supported.");

            PexAssume.IsTrue(index >= 0);
            PexAssume.IsTrue(index < content.Length);
            string result = StringExtensions.GetLineFromIndex(content, index, out lineIndex);
            return result;
            // TODO: 将断言添加到 方法 StringExtensionsTest.GetLineFromIndex(String, Int32, Int32&)
        }


        private void GetLineFromIndexCore(char newLineSymbol)
        {
            int lineIndex;
            //Test first line
            string result;

            result = StringExtensions.GetLineFromIndex($"{newLineSymbol}{newLineSymbol}world", 1, out lineIndex);
            PexAssert.AreEqual(newLineSymbol.ToString(), result);
            PexAssert.AreEqual(1, lineIndex);

            result = StringExtensions.GetLineFromIndex($"hello{newLineSymbol}world", 0, out lineIndex);
            PexAssert.AreEqual("hello"+newLineSymbol, result);
            PexAssert.AreEqual(0, lineIndex);

            result = StringExtensions.GetLineFromIndex($"hello{newLineSymbol}world", 1, out lineIndex);
            PexAssert.AreEqual("hello"+newLineSymbol, result);
            PexAssert.AreEqual(0, lineIndex);

            result = StringExtensions.GetLineFromIndex($"hello{newLineSymbol}world", 4, out lineIndex);
            PexAssert.AreEqual("hello"+newLineSymbol, result);
            PexAssert.AreEqual(0, lineIndex);


            //Test on new line symbol
            result = StringExtensions.GetLineFromIndex($"{newLineSymbol}world", 0, out lineIndex);
            PexAssert.AreEqual(newLineSymbol.ToString(), result);
            PexAssert.AreEqual(0, lineIndex);

            result = StringExtensions.GetLineFromIndex($"hello{newLineSymbol}world", 5, out lineIndex);
            PexAssert.AreEqual("hello"+ newLineSymbol, result);
            PexAssert.AreEqual(0, lineIndex);


            result = StringExtensions.GetLineFromIndex($"hello{newLineSymbol}world{newLineSymbol}", 11, out lineIndex);
            PexAssert.AreEqual("world"+newLineSymbol, result);
            PexAssert.AreEqual(6, lineIndex);





            //Test last line
            result = StringExtensions.GetLineFromIndex($"hello{newLineSymbol}world", 6, out lineIndex);
            PexAssert.AreEqual("world", result);
            PexAssert.AreEqual(6, lineIndex);

            result = StringExtensions.GetLineFromIndex($"hello{newLineSymbol}world", 7, out lineIndex);
            PexAssert.AreEqual("world", result);
            PexAssert.AreEqual(6, lineIndex);


            result = StringExtensions.GetLineFromIndex($"hello{newLineSymbol}world", 10, out lineIndex);
            PexAssert.AreEqual("world", result);
            PexAssert.AreEqual(6, lineIndex);

            //Test middle line
            result = StringExtensions.GetLineFromIndex($"Happy{newLineSymbol}Hacking{newLineSymbol}Keyboard", 6, out lineIndex);
            PexAssert.AreEqual("Hacking" + newLineSymbol, result);
            PexAssert.AreEqual(6, lineIndex);

            result = StringExtensions.GetLineFromIndex($"Happy{newLineSymbol}Hacking{newLineSymbol}Keyboard", 7, out lineIndex);
            PexAssert.AreEqual("Hacking" + newLineSymbol, result);
            PexAssert.AreEqual(6, lineIndex);

            result = StringExtensions.GetLineFromIndex($"Happy{newLineSymbol}Hacking{newLineSymbol}Keyboard", 12, out lineIndex);
            PexAssert.AreEqual("Hacking" + newLineSymbol, result);
            PexAssert.AreEqual(6, lineIndex);
        }

        [TestMethod]
        public void GetLineFromIndexTest()
        {
            GetLineFromIndexCore('\r');
            GetLineFromIndexCore('\n');

            int lineIndex;

            //Test first line
            string result = StringExtensions.GetLineFromIndex("hello\r\nworld", 0, out lineIndex);
            PexAssert.AreEqual("hello\r\n", result);
            PexAssert.AreEqual(0, lineIndex);

            result = StringExtensions.GetLineFromIndex("hello\r\nworld", 1, out lineIndex);
            PexAssert.AreEqual("hello\r\n", result);
            PexAssert.AreEqual(0, lineIndex);

            result = StringExtensions.GetLineFromIndex("hello\r\nworld", 4, out lineIndex);
            PexAssert.AreEqual("hello\r\n", result);
            PexAssert.AreEqual(0, lineIndex);

            //Test on new line symbol
            result = StringExtensions.GetLineFromIndex("\r\nhello\r\nworld", 0, out lineIndex);
            PexAssert.AreEqual("\r\n", result);
            PexAssert.AreEqual(0, lineIndex);

            result = StringExtensions.GetLineFromIndex("\r\nhello\r\nworld", 1, out lineIndex);
            PexAssert.AreEqual("\r\n", result);
            PexAssert.AreEqual(0, lineIndex);


            result = StringExtensions.GetLineFromIndex("hello\r\nworld", 5, out lineIndex);
            PexAssert.AreEqual("hello\r\n", result);
            PexAssert.AreEqual(0, lineIndex);

            result = StringExtensions.GetLineFromIndex("hello\r\nworld", 6, out lineIndex);
            PexAssert.AreEqual("hello\r\n", result);
            PexAssert.AreEqual(0, lineIndex);

            //Test last line
            result = StringExtensions.GetLineFromIndex("hello\r\nworld", 7, out lineIndex);
            PexAssert.AreEqual("world", result);
            PexAssert.AreEqual(7, lineIndex);

            result = StringExtensions.GetLineFromIndex("hello\r\nworld", 8, out lineIndex);
            PexAssert.AreEqual("world", result);
            PexAssert.AreEqual(7, lineIndex);


            result = StringExtensions.GetLineFromIndex("hello\r\nworld", 11, out lineIndex);
            PexAssert.AreEqual("world", result);
            PexAssert.AreEqual(7, lineIndex);

            //Test middle line
            result = StringExtensions.GetLineFromIndex("hello\r\nworld\r\n", 11, out lineIndex);
            PexAssert.AreEqual("world\r\n", result);
            PexAssert.AreEqual(7, lineIndex);

            result = StringExtensions.GetLineFromIndex("Happy\r\nHacking\r\nKeyboard", 7, out lineIndex);
            PexAssert.AreEqual("Hacking\r\n", result);
            PexAssert.AreEqual(7, lineIndex);

            result = StringExtensions.GetLineFromIndex("Happy\r\nHacking\r\nKeyboard", 8, out lineIndex);
            PexAssert.AreEqual("Hacking\r\n", result);
            PexAssert.AreEqual(7, lineIndex);

            result = StringExtensions.GetLineFromIndex("Happy\r\nHacking\r\nKeyboard", 13, out lineIndex);
            PexAssert.AreEqual("Hacking\r\n", result);
            PexAssert.AreEqual(7, lineIndex);


            result = StringExtensions.GetLineFromIndex("a\r\nb\r\n", 4, out lineIndex);
            PexAssert.AreEqual("b\r\n", result);
            PexAssert.AreEqual(3, lineIndex);

        }


    }
}
