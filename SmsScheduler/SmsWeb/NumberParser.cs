using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ConfigurationModels;

namespace SmsWeb
{
    public class NumberParser
    {
        public CountryCodeReplacement CountryCodeReplacement { get; set; }

        public NumberParser(CountryCodeReplacement countryCodeReplacement)
        {
            CountryCodeReplacement = countryCodeReplacement;
        }

        public List<string> InternationaliseAndClean(string[] numberList)
        {
            var internationalisedNumbers = new List<string>();
            if (CountryCodeReplacement != null && CountryCodeReplacement.IsValid)
            {
                var countryCodeRegex = new Regex(CountryCodeReplacement.LeadingNumberToReplace);
                foreach (var number in numberList)
                {
                    var cleanNumber = number.Trim();
                    if (cleanNumber.StartsWith(CountryCodeReplacement.LeadingNumberToReplace, StringComparison.CurrentCultureIgnoreCase))
                    {
                        var replace = countryCodeRegex.Replace(cleanNumber, CountryCodeReplacement.CountryCode, 1);
                        internationalisedNumbers.Add(replace);
                    }
                    else
                    {
                        internationalisedNumbers.Add(cleanNumber);
                    }
                }
            }
            else
            {
                internationalisedNumbers.AddRange(numberList.Select(number => number.Trim()));
            }
            return internationalisedNumbers;
        }
    }
}