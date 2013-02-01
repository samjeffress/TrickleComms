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
    }
}
