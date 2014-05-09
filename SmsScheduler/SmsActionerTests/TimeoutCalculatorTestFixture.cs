using System;
using NUnit.Framework;
using SmsActioner;

namespace SmsActionerTests
{
    [TestFixture]
    public class TimeoutCalculatorTestFixture
    {
        [Test]
        public void NoTimeoutsSet_Return10Seconds()
        {
            var timeoutCalculator = new TimeoutCalculator();
            const int numberOfTimeoutsComplete = 0;
            var timeout = timeoutCalculator.RequiredTimeout(numberOfTimeoutsComplete);
            Assert.That(timeout, Is.EqualTo(new TimeSpan(0,0,10)));
        }

        [Test]
        public void OneTimeoutSet_Return30Seconds()
        {
            var timeoutCalculator = new TimeoutCalculator();
            const int numberOfTimeoutsComplete = 1;
            var timeout = timeoutCalculator.RequiredTimeout(numberOfTimeoutsComplete);
            Assert.That(timeout, Is.EqualTo(new TimeSpan(0,0,30)));
        }

        [Test]
        public void TwoTimeoutsSet_Return60Seconds()
        {
            var timeoutCalculator = new TimeoutCalculator();
            const int numberOfTimeoutsComplete = 2;
            var timeout = timeoutCalculator.RequiredTimeout(numberOfTimeoutsComplete);
            Assert.That(timeout, Is.EqualTo(new TimeSpan(0,1,0)));
        }

        [Test]
        public void ThreeTimeoutsSet_Return5Minutes()
        {
            var timeoutCalculator = new TimeoutCalculator();
            const int numberOfTimeoutsComplete = 3;
            var timeout = timeoutCalculator.RequiredTimeout(numberOfTimeoutsComplete);
            Assert.That(timeout, Is.EqualTo(new TimeSpan(0,5,0)));
        }

        [Test]
        public void FourTimeoutsSet_Return30Minutes()
        {
            var timeoutCalculator = new TimeoutCalculator();
            const int numberOfTimeoutsComplete = 4;
            var timeout = timeoutCalculator.RequiredTimeout(numberOfTimeoutsComplete);
            Assert.That(timeout, Is.EqualTo(new TimeSpan(0,30,0)));
        }

        [Test]
        public void FiveTimeoutsSet_Return60Minutes()
        {
            var timeoutCalculator = new TimeoutCalculator();
            const int numberOfTimeoutsComplete = 5;
            var timeout = timeoutCalculator.RequiredTimeout(numberOfTimeoutsComplete);
            Assert.That(timeout, Is.EqualTo(new TimeSpan(0,60,0)));
        }

        [Test]
        public void OverFiveTimeoutsSet_Return60Minutes()
        {
            var timeoutCalculator = new TimeoutCalculator();
            const int numberOfTimeoutsComplete = 24;
            var timeout = timeoutCalculator.RequiredTimeout(numberOfTimeoutsComplete);
            Assert.That(timeout, Is.EqualTo(new TimeSpan(0,60,0)));
        }
    }
}