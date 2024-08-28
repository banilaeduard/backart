using System;
using System.Linq;

namespace NER
{
    public class WordTokenizer
    {
        private static char[] splitChar = [' ', ',', '.', ':', '!'];
        static WordTokenizer()
        {

        }

        public string[] Tokenize(string textBody)
        {
            if (string.IsNullOrWhiteSpace(textBody)) return Array.Empty<string>();

            return textBody.Split(splitChar, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToArray();
        }
    }
}