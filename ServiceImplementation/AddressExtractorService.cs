using NER;

namespace AddressExtractorImpl
{
    public class AddressExtractorService
    {
        private static readonly string[] punctuation = { ".", ":", "!", ",", "-" };
        private static readonly string[] address = { "jud", "judet", "com", "comuna", "municipiul", "mun",
                                                                    "str", "strada", "oras", "soseaua", "valea",
                                                                    "sat", "satu", "cod postal", "postal code",
                                                                    "bulevardul", "bulevard", "bdul", "bld-ul", "b-dul",
                                                                    "calea", "aleea", "sos", "sect", "sectorul", "sector" };

        public async Task<string[]> Parse(string[] sentences)
        {
            HashSet<string> addressEntry = new();
            string nrComanda = String.Empty;
            WordTokenizer wordTok = new();
            var addressLine = string.Empty;

            foreach (var sent in sentences)
            {

                bool shouldProcess = false;
                bool ignoreNextPunctaion = false;
                bool nearAddress = false;
                bool postalCode = true;
                bool wasCountry = false;
                int wasNumber = 0;
                int number = 0;
                int addressHits = 0;
                int lastAddressIndex = -1;
                int offsetAddressIndex = 0;
                var words = wordTok.Tokenize(sent);

                for (int i = 0; i < words.Length; i++)
                {
                    var word = words[i];
                    nearAddress = lastAddressIndex != -1
                        && (i - lastAddressIndex + offsetAddressIndex) < 3;

                    if (!nearAddress
                        || string.Equals(word, "tel", comparisonType: StringComparison.InvariantCultureIgnoreCase)
                          || string.Equals(word, "telefon", comparisonType: StringComparison.InvariantCultureIgnoreCase)
                            || string.Equals(word, "telephone", comparisonType: StringComparison.InvariantCultureIgnoreCase)
                                || string.Equals(word, "phone", comparisonType: StringComparison.InvariantCultureIgnoreCase)
                          )
                    {
                        if (!string.IsNullOrWhiteSpace(addressLine) &&
                            !addressEntry.Contains(addressLine.Trim()) && addressHits > 1)
                        {
                            addressEntry.Add(addressLine.Trim().Trim(punctuation.Select(t => t[0]).ToArray()));
                        }
                        addressLine = string.Empty;
                        shouldProcess = false;
                        ignoreNextPunctaion = false;
                        postalCode = true;
                        wasCountry = false;
                        wasNumber = 0;
                        number = 0;
                        addressHits = 0;
                        lastAddressIndex = -1;
                    }

                    if (address.Contains(word, StringComparer.InvariantCultureIgnoreCase)
                        || (word.Contains("nr", StringComparison.InvariantCultureIgnoreCase) && nearAddress))
                    {
                        if (word.Contains("nr", StringComparison.InvariantCultureIgnoreCase))
                        {
                            wasNumber++;
                        }
                        if (wasNumber < 2)
                        {
                            addressLine += word + " ";
                            shouldProcess = true;
                            ignoreNextPunctaion = true;
                            lastAddressIndex = i + offsetAddressIndex;
                            postalCode = true;
                            addressHits++;
                        }
                    }
                    else if (shouldProcess)
                    {
                        if (Char.IsLetterOrDigit(word[0]))
                        {
                            addressLine += word + " ";
                        }
                        else if (ignoreNextPunctaion)
                            addressLine += word + " ";
                        else
                            shouldProcess = false;

                        lastAddressIndex = i + offsetAddressIndex;
                        ignoreNextPunctaion = false;
                    }
                    else if (nearAddress && !wasCountry && wasNumber < 2)
                    {
                        if (punctuation.Contains(word))
                        {
                            addressLine += word + " ";
                            lastAddressIndex++;
                        }
                        else if (int.TryParse(word, out number) && postalCode)
                        {
                            addressLine += number + " ";
                            postalCode = false;
                        }
                        else
                        {
                            if (new string[] { "RO", "Romania" }.Contains(word, StringComparer.InvariantCultureIgnoreCase))
                            {
                                wasCountry = true;
                            }
                            addressLine += word + " ";
                        }
                    }
                    else
                    {
                        lastAddressIndex = -1;
                    }
                }

                offsetAddressIndex += words.Length - 1;

                if (!string.IsNullOrWhiteSpace(addressLine) &&
                                !addressEntry.Contains(addressLine.Trim()) && addressHits > 1)
                    addressEntry.Add(addressLine.Trim());
            }

            return addressEntry.ToArray();
        }
    }
}