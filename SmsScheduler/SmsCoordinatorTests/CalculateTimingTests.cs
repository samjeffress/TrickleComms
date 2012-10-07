using System;
using NUnit.Framework;
using SmsCoordinator;

namespace SmsCoordinatorTests
{
    [TestFixture]
    public class CalculateTimingTests
    {
        [Test]
        public void CalculateTimingOverOneToSchedule()
        {
            var startTime = DateTime.Now;
            var duration = new TimeSpan(75);
            const int numberOfItemsToSchedule = 3;

            var calculateSmsTiming = new CalculateSmsTiming();
            var messageTimes = calculateSmsTiming.CalculateTiming(startTime, duration, numberOfItemsToSchedule);

            Assert.That(messageTimes.Count, Is.EqualTo(numberOfItemsToSchedule));
            Assert.That(messageTimes[0].Ticks, Is.EqualTo(startTime.Ticks));
            Assert.That(messageTimes[1].Ticks, Is.EqualTo(startTime.Ticks + duration.Ticks / 2));
            Assert.That(messageTimes[2].Ticks, Is.EqualTo(startTime.Ticks + duration.Ticks));
        }

        [Test]
        public void CalculateTimingOneItemToSchedule()
        {
            var startTime = DateTime.Now;
            var duration = new TimeSpan(75);
            const int numberOfItemsToSchedule = 1;

            var calculateSmsTiming = new CalculateSmsTiming();
            var messageTimes = calculateSmsTiming.CalculateTiming(startTime, duration, numberOfItemsToSchedule);

            Assert.That(messageTimes.Count, Is.EqualTo(1));
            Assert.That(messageTimes[0].Ticks, Is.EqualTo(startTime.Ticks));
        }
    }
}