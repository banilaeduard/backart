using System.Linq;

namespace NER
{
    public class SentenceTokenizer
    {
        static SentenceTokenizer()
        {

        }

        public string[] DetectSentences(string textBody)
        {
            if (string.IsNullOrWhiteSpace(textBody)) return System.Array.Empty<string>();
            return PragmaticSegmenterNet.Segmenter.Segment(textBody).ToArray();
        }
    }
}
