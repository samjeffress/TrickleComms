using System;
using System.Collections.Generic;
using System.Linq;
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
                    Numbers = "04040404040, 11111111111",
                    Message = "Message",
                    StartTime = DateTime.Now.AddHours(2),
                    TimeSeparatorSeconds = 90,
                    Tags = "tag1, tag2",
                    Topic = "Dance Dance Revolution!",
                    ConfirmationEmail = "confirmation"
                };
            var mapper = new CoordinatorModelToMessageMapping();
            var message = mapper.MapToTrickleSpacedByPeriod(model);

            Assert.That(message.Messages.Count, Is.EqualTo(2));
            Assert.That(message.Messages[0].Mobile, Is.EqualTo(model.Numbers.Split(',')[0].Trim()));
            Assert.That(message.Messages[0].Message, Is.EqualTo(model.Message));
            Assert.That(message.Messages[1].Mobile, Is.EqualTo(model.Numbers.Split(',')[1].Trim()));
            Assert.That(message.Messages[1].Message, Is.EqualTo(model.Message));
            Assert.That(message.MetaData.Tags, Is.EqualTo(model.Tags.Split(',').ToList().Select(t => t.Trim()).ToList()));
            Assert.That(message.MetaData.Topic, Is.EqualTo(model.Topic));
            Assert.That(message.StartTimeUtc, Is.EqualTo(model.StartTime.ToUniversalTime()));
            Assert.That(message.TimeSpacing, Is.EqualTo(TimeSpan.FromSeconds(model.TimeSeparatorSeconds.Value)));
            Assert.That(message.ConfirmationEmail, Is.EqualTo(model.ConfirmationEmail));
        }

        [Test]
        public void MapToTrickleOverTimePeriod()
        {
            var model = new CoordinatedSharedMessageModel
                {
                    Numbers = "04040404040, 11111111111",
                    Message = "Message",
                    StartTime = DateTime.Now.AddHours(2),
                    SendAllBy = DateTime.Now.AddHours(3),
                    Tags = "tag1, tag2",
                    Topic = "Dance Dance Revolution!",
                    ConfirmationEmail = "toby@toby.com"
                };
            var mapper = new CoordinatorModelToMessageMapping();
            var message = mapper.MapToTrickleOverPeriod(model);

            var coordinationDuration = model.SendAllBy.Value.Subtract(model.StartTime);
            Assert.That(coordinationDuration, Is.GreaterThan(new TimeSpan(0)));
            Assert.That(message.Messages.Count, Is.EqualTo(2));
            Assert.That(message.Messages[0].Mobile, Is.EqualTo(model.Numbers.Split(',')[0].Trim()));
            Assert.That(message.Messages[0].Message, Is.EqualTo(model.Message));
            Assert.That(message.Messages[1].Mobile, Is.EqualTo(model.Numbers.Split(',')[1].Trim()));
            Assert.That(message.Messages[1].Message, Is.EqualTo(model.Message));
            Assert.That(message.MetaData.Tags, Is.EqualTo(model.Tags.Split(',').ToList().Select(t => t.Trim().ToList())));
            Assert.That(message.MetaData.Topic, Is.EqualTo(model.Topic));
            Assert.That(message.StartTimeUtc, Is.EqualTo(model.StartTime.ToUniversalTime()));
            Assert.That(message.Duration, Is.EqualTo(coordinationDuration));
            Assert.That(message.ConfirmationEmail, Is.EqualTo(model.ConfirmationEmail));
        }

        [Test]
        public void MapToTrickleOverTimePeriodWithoutTags()
        {
            var model = new CoordinatedSharedMessageModel
                {
                    Numbers = "04040404040, 11111111111",
                    Message = "Message",
                    StartTime = DateTime.Now.AddHours(2),
                    SendAllBy = DateTime.Now.AddHours(3),
                    Topic = "Dance Dance Revolution!",
                    ConfirmationEmail = "toby@toby.com"
                };
            var mapper = new CoordinatorModelToMessageMapping();
            var message = mapper.MapToTrickleOverPeriod(model);

            var coordinationDuration = model.SendAllBy.Value.Subtract(model.StartTime);
            Assert.That(coordinationDuration, Is.GreaterThan(new TimeSpan(0)));
            Assert.That(message.Messages.Count, Is.EqualTo(2));
            Assert.That(message.Messages[0].Mobile, Is.EqualTo(model.Numbers.Split(',')[0].Trim()));
            Assert.That(message.Messages[0].Message, Is.EqualTo(model.Message));
            Assert.That(message.Messages[1].Mobile, Is.EqualTo(model.Numbers.Split(',')[1].Trim()));
            Assert.That(message.Messages[1].Message, Is.EqualTo(model.Message));
            Assert.That(message.MetaData.Tags, Is.EqualTo(null));
            Assert.That(message.MetaData.Topic, Is.EqualTo(model.Topic));
            Assert.That(message.StartTimeUtc, Is.EqualTo(model.StartTime.ToUniversalTime()));
            Assert.That(message.Duration, Is.EqualTo(coordinationDuration));
            Assert.That(message.ConfirmationEmail, Is.EqualTo(model.ConfirmationEmail));
        }
    }
}