using System;
using System.Collections.Generic;
using NUnit.Framework;
using SmsWeb.Controllers;
using SmsWeb.Models;

namespace SmsWebTests
{
    [TestFixture]
    public class CoordinatorModelToMessageMappingTestFixture
    {
        [Test]
        public void MapToTrickleSpacedByTimePeriod()
        {
            var model = new CoordinatedSharedMessageModel
                {
                    Numbers = new List<string> { "04040404040", "11111111111"},
                    Message = "Message",
                    StartTime = DateTime.Now.AddHours(2),
                    TimeSeparator = new TimeSpan(5000)
                };
            var mapper = new CoordinatorModelToMessageMapping();
            var message = mapper.MapToTrickleSpacedByPeriod(model);

            Assert.That(message.Messages.Count, Is.EqualTo(model.Numbers.Count));
            Assert.That(message.Messages[0].Mobile, Is.EqualTo(model.Numbers[0]));
            Assert.That(message.Messages[0].Message, Is.EqualTo(model.Message));
            Assert.That(message.Messages[1].Mobile, Is.EqualTo(model.Numbers[1]));
            Assert.That(message.Messages[1].Message, Is.EqualTo(model.Message));
            // TODO: Allow metadata to be set
            //Assert.That(trickleByTime.MetaData);
            Assert.That(message.StartTime, Is.EqualTo(model.StartTime));
            Assert.That(message.TimeSpacing, Is.EqualTo(model.TimeSeparator));
        }

        [Test]
        public void MapToTrickleOverTimePeriod()
        {
            var model = new CoordinatedSharedMessageModel
                {
                    Numbers = new List<string> { "04040404040", "11111111111"},
                    Message = "Message",
                    StartTime = DateTime.Now.AddHours(2),
                    SendAllBy = DateTime.Now.AddHours(3)
                };
            var mapper = new CoordinatorModelToMessageMapping();
            var message = mapper.MapToTrickleOverPeriod(model);

            var coordinationDuration = model.SendAllBy.Value.Subtract(model.StartTime);
            Assert.That(coordinationDuration, Is.GreaterThan(new TimeSpan(0)));
            Assert.That(message.Messages.Count, Is.EqualTo(model.Numbers.Count));
            Assert.That(message.Messages[0].Mobile, Is.EqualTo(model.Numbers[0]));
            Assert.That(message.Messages[0].Message, Is.EqualTo(model.Message));
            Assert.That(message.Messages[1].Mobile, Is.EqualTo(model.Numbers[1]));
            Assert.That(message.Messages[1].Message, Is.EqualTo(model.Message));
            // TODO: Allow metadata to be set
            //Assert.That(trickleByTime.MetaData);
            Assert.That(message.StartTime, Is.EqualTo(model.StartTime));
            Assert.That(message.Duration, Is.EqualTo(coordinationDuration));
        }
    }
}