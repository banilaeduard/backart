using System.Text;
using System.Text.RegularExpressions;

namespace NumarComandaExtractor
{
    public class NumarComandaExtractorService
    {
        private static Regex nrComdanda = new Regex(@"4\d{9}");

        public async Task<string> Extract(string items)
        {
            var match = nrComdanda.Match(items);
            StringBuilder stringBuilder = new();

            if (match.Success)
            {
                for (int i = 0; i < match.Groups.Count; i++)
                {
                    stringBuilder.AppendLine(match.Groups[i].Value);
                }
            }

            return stringBuilder.ToString();
        }
    }
}
