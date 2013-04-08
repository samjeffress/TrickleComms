using System;
using NodaTime;

namespace EmailSender
{
    public interface IDateTimeOlsenFromUtcMapping
    {
        DateTime DateTimeUtcToLocalWithOlsenZone(DateTime dateTimeUtc, string olsenTimeZone);
    }

    public class DateTimeUtcFromOlsenMapping : IDateTimeOlsenFromUtcMapping
    {
        public DateTime DateTimeUtcToLocalWithOlsenZone(DateTime dateTimeUtc, string olsenTimeZone)
        {
            var dateTimeZoneProvider = DateTimeZoneProviders.Tzdb;
            var dateTimeZone = dateTimeZoneProvider[olsenTimeZone];
            //var utcInstant = new Instant(dateTimeUtc.Ticks);
            var utcInstant = Instant.FromDateTimeUtc(dateTimeUtc);
            var zonedDateTime = new ZonedDateTime(utcInstant, dateTimeZone);
            return zonedDateTime.ToDateTimeUnspecified();
        }
    }
}