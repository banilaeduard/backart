using NER;

namespace Tokenizer
{
    public class TokenizerService
    {
        SentenceTokenizer sentTok = new();
        WordTokenizer wordTok = new();

        public async Task<string[]> GetSentences(string text)
        {
            return sentTok.DetectSentences(text);
        }

        public async Task<string[]> GetWords(string text)
        {
            return wordTok.Tokenize(text);
        }

        public async Task<string> HtmlStrip(string html)
        {
            return HtmlStripper.StripHtml(html);
        }
    }
}
