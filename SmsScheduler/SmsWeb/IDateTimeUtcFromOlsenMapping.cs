using System;
using NodaTime;

namespace SmsWeb
{
    public interface IDateTimeUtcFromOlsenMapping
    {
        DateTime DateTimeWithOlsenZoneToUtc(DateTime dateTime, string olsenTimeZone);
    }

    public class DateTimeUtcFromOlsenMapping : IDateTimeUtcFromOlsenMapping
    {
        public DateTime DateTimeWithOlsenZoneToUtc(DateTime dateTime, string olsenTimeZone)
        {
            var dateTimeZoneProvider = DateTimeZoneProviders.Tzdb;
            var dateTimeZone = dateTimeZoneProvider[olsenTimeZone];
            var startTime = dateTime;
            var localDateTime = new LocalDateTime(startTime.Year, startTime.Month, startTime.Day, startTime.Hour, startTime.Minute);
            return dateTimeZone.AtLeniently(localDateTime).ToDateTimeUtc();
        }
    }
}