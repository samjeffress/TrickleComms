using System;
using EmailSender;
using NUnit.Framework;              

namespace EmailSenderTests
{
    [TestFixture]
    public class UtcToLocalDateTimeTestFixture
    {
        [Test]
        public void UtcToElSalvadorTest()
        {
            var dateTimeUtcFromOlsenMapping = new DateTimeUtcFromOlsenMapping();
            var utcNow = DateTime.UtcNow;
            var elSalvadorNow = utcNow.AddHours(-6);
            const string timeZone = "America/El_Salvador";
            //const string timeZone = "Australia/Sydney";
            var elSalvadorMapped = dateTimeUtcFromOlsenMapping.DateTimeUtcToLocalWithOlsenZone(utcNow, timeZone);

            Assert.That(elSalvadorMapped.Date.Year, Is.EqualTo(elSalvadorNow.Date.Year));
            Assert.That(elSalvadorMapped.Date.Month, Is.EqualTo(elSalvadorNow.Date.Month));
            Assert.That(elSalvadorMapped.Date.Day, Is.EqualTo(elSalvadorNow.Date.Day));
            Assert.That(elSalvadorMapped.Date.Hour, Is.EqualTo(elSalvadorNow.Date.Hour));
            Assert.That(elSalvadorMapped.Date.Minute, Is.EqualTo(elSalvadorNow.Date.Minute));
        }
    }
}