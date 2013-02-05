using System;
using System.Collections.Generic;

namespace SmsCoordinator
{
    public interface ICalculateSmsTiming
    {
        List<DateTime> CalculateTiming(DateTime startTime, TimeSpan duration, int numberOfItemsToSchedule);
    }

    public class CalculateSmsTiming : ICalculateSmsTiming
    {
        public List<DateTime> CalculateTiming(DateTime startTime, TimeSpan duration, int numberOfItemsToSchedule)
        {
            var dateTimes = new List<DateTime> { startTime };
            if (numberOfItemsToSchedule == 1)
                return dateTimes;

            if (numberOfItemsToSchedule > 2)
            {
                var durationSplit = duration.Ticks / (numberOfItemsToSchedule - 1);
                for (int i = 0; i < numberOfItemsToSchedule - 2; i++)
                {
                    var dateTime = new DateTime(startTime.Ticks + durationSplit*(i + 1), DateTimeKind.Utc);
                    dateTimes.Add(dateTime);
                }
            }

            dateTimes.Add(startTime.Add(duration));
            return dateTimes;
        }
    }
}