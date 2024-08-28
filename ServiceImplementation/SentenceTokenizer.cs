namespace NER
{
    internal class SentenceTokenizer
    {
        static SentenceTokenizer()
        {

        }

        internal string[] DetectSentences(string textBody)
        {
            if (string.IsNullOrWhiteSpace(textBody)) return System.Array.Empty<string>();
            return PragmaticSegmenterNet.Segmenter.Segment(textBody).ToArray();
        }
    }
}
