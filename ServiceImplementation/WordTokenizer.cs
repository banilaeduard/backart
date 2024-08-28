namespace NER
{
    internal class WordTokenizer
    {
        private static char[] splitChar = [' ', ',', '.', ':', '!'];
        static WordTokenizer()
        {

        }

        internal string[] Tokenize(string textBody)
        {
            if (string.IsNullOrWhiteSpace(textBody)) return Array.Empty<string>();

            return textBody.Split(splitChar, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToArray();
        }
    }
}