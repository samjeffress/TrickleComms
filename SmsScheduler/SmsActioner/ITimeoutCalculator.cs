using System;
using System.Collections.Generic;

namespace SmsActioner
{
    public interface ITimeoutCalculator
    {
        TimeSpan RequiredTimeout(int numberOfTimeoutsComplete);
    }

    public class TimeoutCalculator : ITimeoutCalculator
    {
        private readonly List<TimeSpan> _timespans = new List<TimeSpan>
            {
                new TimeSpan(0, 0, 10),
                new TimeSpan(0, 0, 30),
                new TimeSpan(0, 1, 0),
                new TimeSpan(0, 5, 0),
                new TimeSpan(0, 30, 0),
                new TimeSpan(0, 60, 0)
            };

        public TimeSpan RequiredTimeout(int numberOfTimeoutsComplete)
        {
            if (numberOfTimeoutsComplete < 0)
                numberOfTimeoutsComplete = 0;
            if (numberOfTimeoutsComplete >= _timespans.Count)
                numberOfTimeoutsComplete = _timespans.Count - 1;
            return _timespans[numberOfTimeoutsComplete];
        }
    }
}