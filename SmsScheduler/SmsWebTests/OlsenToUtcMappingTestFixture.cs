using System;
using NUnit.Framework;
using SmsWeb;
using SmsWeb.Controllers;

namespace SmsWebTests
{
    [TestFixture]
    public class OlsenToUtcMappingTestFixture
    {
        [Test]
        public void MapToTrickleOverTimeTimeZoneTest()
        {
            var startTimeElSalvador = DateTime.UtcNow;
            const string timeZone = "America/El_Salvador";
            var startTimeUtc = startTimeElSalvador.AddHours(6);

            var mapper = new DateTimeUtcFromOlsenMapping();
            var dateTimeUtc = mapper.DateTimeWithOlsenZoneToUtc(startTimeElSalvador, timeZone);

            Assert.That(dateTimeUtc.Date.Year, Is.EqualTo(startTimeUtc.Date.Year));
            Assert.That(dateTimeUtc.Date.Month, Is.EqualTo(startTimeUtc.Date.Month));
            Assert.That(dateTimeUtc.Date.Day, Is.EqualTo(startTimeUtc.Date.Day));
            Assert.That(dateTimeUtc.Hour, Is.EqualTo(startTimeUtc.Hour));
            Assert.That(dateTimeUtc.Minute, Is.EqualTo(startTimeUtc.Minute));
        }

        [Test]
        public void MapToTrickleOverTimeTimeZoneTestUTC()
        {
            var startTimeUtc = DateTime.UtcNow;
            const string timeZone = "UTC";

            var mapper = new DateTimeUtcFromOlsenMapping();
            var dateTimeUtc = mapper.DateTimeWithOlsenZoneToUtc(startTimeUtc, timeZone);

            Assert.That(dateTimeUtc.Date.Year, Is.EqualTo(startTimeUtc.Date.Year));
            Assert.That(dateTimeUtc.Date.Month, Is.EqualTo(startTimeUtc.Date.Month));
            Assert.That(dateTimeUtc.Date.Day, Is.EqualTo(startTimeUtc.Date.Day));
            Assert.That(dateTimeUtc.Hour, Is.EqualTo(startTimeUtc.Hour));
            Assert.That(dateTimeUtc.Minute, Is.EqualTo(startTimeUtc.Minute));
        }
    }
}