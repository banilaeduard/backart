namespace NER
{
    using java.io;
    using opennlp.tools.tokenize;
    public class WordTokenizer
    {
        private TokenizerME tokenizer;
        private string trainModelPath = "./NER/trainmodels/en-token.bin";
        public WordTokenizer()
        {
            InputStream inputStreamTokenizer = new FileInputStream(trainModelPath);
            tokenizer = new TokenizerME(new TokenizerModel(inputStreamTokenizer));
        }

        public string[] Tokenize(string textBody)
        {
            if (string.IsNullOrWhiteSpace(textBody)) return new string[0];

            return tokenizer.tokenize(textBody);
        }
    }
}
