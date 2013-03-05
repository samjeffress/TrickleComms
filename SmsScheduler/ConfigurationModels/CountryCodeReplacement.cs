using System.Text.RegularExpressions;

namespace ConfigurationModels
{
    public class CountryCodeReplacement
    {
        public string CountryCode { get; set; }

        public string LeadingNumberToReplace { get; set; }

        public bool IsValid
        {
            get
            {
                if (string.IsNullOrWhiteSpace(CountryCode) || string.IsNullOrWhiteSpace(LeadingNumberToReplace))
                    return false;
                return true;
            }
        }

        public string CleanAndInternationaliseNumber(string number)
        {
            var cleanNumber = number.Trim();
            if (IsValid)
            {
                var regex = new Regex(LeadingNumberToReplace);
                var leadingNumberReplacement = regex.Replace(cleanNumber.Substring(0, 1), CountryCode, 1);
                return leadingNumberReplacement + cleanNumber.Substring(1);
            }
            return cleanNumber;
        }
    }
}
