namespace NER
{
    using java.io;
    using opennlp.tools.namefind;
    public class NameFinder
    {
        private NameFinderME nameFinder;
        private string trainModelPath = "./NER/trainmodels/ner-custom-model.bin";
        public NameFinder()
        {
            var tst2 = new FileInputStream(trainModelPath); // our trainded model
            nameFinder = new NameFinderME(new TokenNameFinderModel(tst2));
        }

        public NameFinderModel[] getNames(string[] tokens)
        {
            var spans = nameFinder.find(tokens);
            NameFinderModel[] models = new NameFinderModel[spans.Length];

            for (int i = 0; i < spans.Length; i++)
                models[i] = new NameFinderModel()
                {
                    Probability = spans[i].getProb(),
                    Type = spans[i].getType(),
                    Value = tokens[spans[i].getStart()]
                };

            return models;
        }
    }
}
