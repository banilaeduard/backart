using System;
using System.Linq;

namespace NER
{
    public class WordTokenizer
    {
        private static char[] splitChar = new char[] { ' ', ',', '.', ':', '!' };
        static WordTokenizer()
        {

        }

        public string[] Tokenize(string textBody)
        {
            if (string.IsNullOrWhiteSpace(textBody)) return new string[0];

            return textBody.Split(splitChar, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToArray();
        }
    }
}