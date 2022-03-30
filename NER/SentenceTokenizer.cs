namespace NER
{
    using java.io;
    using opennlp.tools.sentdetect;

    public class SentenceTokenizer
    {
        private SentenceDetectorME detector;
        private string trainModelPath = "./NER/trainmodels/en-sent.bin";
        public SentenceTokenizer()
        {
            InputStream inputStream = new FileInputStream(trainModelPath);
            detector = new SentenceDetectorME(new SentenceModel(inputStream));
        }

        public string[] DetectSentences(string textBody)
        {
            if (string.IsNullOrWhiteSpace(textBody)) return new string[0];
            return detector.sentDetect(textBody);
        }
    }
}
