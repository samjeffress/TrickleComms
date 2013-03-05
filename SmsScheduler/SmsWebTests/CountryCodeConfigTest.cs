using ConfigurationModels;
using NUnit.Framework;

namespace SmsWebTests
{
    [TestFixture]
    public class CountryCodeConfigTest
    {
        [Test]
        public void CountryCodeConfigNotSet_LeavesNumber()
        {
            var countryCodeReplacement = new CountryCodeReplacement();
            const string number = "+61400000";
            var cleanAndInternationaliseNumber = countryCodeReplacement.CleanAndInternationaliseNumber(number);

            Assert.That(cleanAndInternationaliseNumber, Is.EqualTo(number));
        }

        [Test]
        public void CountryCodeConfigSet_AdjustsNumberWithLeadingDigit()
        {
            var countryCodeReplacement = new CountryCodeReplacement { CountryCode = "+61", LeadingNumberToReplace = "0" };
            const string number = "0400000";
            var cleanAndInternationaliseNumber = countryCodeReplacement.CleanAndInternationaliseNumber(number);

            Assert.That(cleanAndInternationaliseNumber, Is.EqualTo("+61400000"));
        }

        [Test]
        public void CountryCodeConfigSet_LeavesNumberLeadingDigitDoesntMatch()
        {
            var countryCodeReplacement = new CountryCodeReplacement { CountryCode = "+61", LeadingNumberToReplace = "0" };
            const string number = "+61400000";
            var cleanAndInternationaliseNumber = countryCodeReplacement.CleanAndInternationaliseNumber(number);

            Assert.That(cleanAndInternationaliseNumber, Is.EqualTo("+61400000"));
        }
    }
}