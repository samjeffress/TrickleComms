using System;
using System.Collections.Generic;
using NServiceBus;
using NUnit.Framework;
using Rhino.Mocks;
using SmsMessages.Coordinator;
using SmsWeb.API;

namespace SmsWebTests
{
    [TestFixture]
    public class ApiCoordinatorTestFixture
    {
        [Test]
        public void PostInvalidNeitherTimeSeparatorOrSendBySet()
        {
            var request = new Coordinator { Message = "msg", Numbers = new List<string> { "1" }, StartTime = DateTime.Now.AddMinutes(3) };
            var service = new CoordinatorService();
            var response = service.OnPost(request) as CoordinatorResponse;

            Assert.That(response.ResponseStatus.ErrorCode, Is.EqualTo("InvalidMessage"));
            Assert.That(response.ResponseStatus.Errors[0].Message, Is.EqualTo("Message must contain either Time Separator OR DateTime to send all messages by."));
        }

        [Test]
        public void PostInvalidBothTimeSeparatorAndSendBySet()
        {
            var request = new Coordinator { Message = "msg", Numbers = new List<string> { "1" }, StartTime = DateTime.Now.AddMinutes(3), SendAllBy = DateTime.Now.AddMinutes(33), TimeSeparator = new TimeSpan(300)};
            var service = new CoordinatorService();
            var response = service.OnPost(request) as CoordinatorResponse;

            Assert.That(response.ResponseStatus.ErrorCode, Is.EqualTo("InvalidMessage"));
            Assert.That(response.ResponseStatus.Errors[0].Message, Is.EqualTo("Message must contain either Time Separator OR DateTime to send all messages by."));
        }

        [Test]
        public void PostInvalidNoMessage()
        {
            var request = new Coordinator { Numbers = new List<string> { "1" }, StartTime = DateTime.Now, SendAllBy = DateTime.Now.AddDays(1) };
            var service = new CoordinatorService();
            var response = service.OnPost(request) as CoordinatorResponse;

            Assert.That(response.ResponseStatus.ErrorCode, Is.EqualTo("InvalidMessage"));
            Assert.That(response.ResponseStatus.Errors[0].Message, Is.EqualTo("Sms Message Required"));
        }

        [Test]
        public void PostInvalidNoNumbers()
        {
            var request = new Coordinator { Message = "msg", StartTime = DateTime.Now, SendAllBy = DateTime.Now.AddDays(1) };
            var service = new CoordinatorService();
            var response = service.OnPost(request) as CoordinatorResponse;

            Assert.That(response.ResponseStatus.ErrorCode, Is.EqualTo("InvalidMessage"));
            Assert.That(response.ResponseStatus.Errors[0].Message, Is.EqualTo("List of numbers required"));
        }

        [Test]
        public void PostInvalidNoStartTime()
        {
            var request = new Coordinator { Message = "msg", Numbers = new List<string> { "1" }, SendAllBy = DateTime.Now.AddDays(1) };
            var service = new CoordinatorService();
            var response = service.OnPost(request) as CoordinatorResponse;

            Assert.That(response.ResponseStatus.ErrorCode, Is.EqualTo("InvalidMessage"));
            Assert.That(response.ResponseStatus.Errors[0].Message, Is.EqualTo("Start time must be set"));
        }

        [Test]
        public void PostInvalidStartTimeInPast()
        {
            var request = new Coordinator { Message = "msg", Numbers = new List<string> { "1" }, StartTime = DateTime.Now.AddDays(-1), SendAllBy = DateTime.Now.AddDays(1) };
            var service = new CoordinatorService();
            var response = service.OnPost(request) as CoordinatorResponse;

            Assert.That(response.ResponseStatus.ErrorCode, Is.EqualTo("InvalidMessage"));
            Assert.That(response.ResponseStatus.Errors[0].Message, Is.EqualTo("Start time must not be in the past"));
        }

        [Test]
        public void PostValidWithEndDateCoordinator()
        {
            var request = new Coordinator { Message = "msg", Numbers = new List<string> { "1" }, StartTime = DateTime.Now.AddDays(1), SendAllBy = DateTime.Now.AddDays(1) };

            var bus = MockRepository.GenerateMock<IBus>();

            var trickleMessage = new TrickleSmsOverCalculatedIntervalsBetweenSetDates();
            bus.Expect(b => b.Send(Arg<TrickleSmsOverCalculatedIntervalsBetweenSetDates>.Is.NotNull))
                .WhenCalled(i => trickleMessage = (TrickleSmsOverCalculatedIntervalsBetweenSetDates)((object[])(i.Arguments[0]))[0]);
            
            var service = new CoordinatorService { Bus = bus};
            var response = service.OnPost(request) as CoordinatorResponse;

            Assert.That(response.ResponseStatus.ErrorCode, Is.Null);
            Assert.That(response.RequestId, Is.Not.EqualTo(Guid.Empty));

            bus.VerifyAllExpectations();
        }

        [Test]
        public void PostValidTimeSeparatedCoordinator()
        {
            var request = new Coordinator { Message = "msg", Numbers = new List<string> { "1" }, StartTime = DateTime.Now.AddDays(1), TimeSeparator = new TimeSpan(0,0,3)};
            var bus = MockRepository.GenerateMock<IBus>();

            var trickleMessage = new TrickleSmsWithDefinedTimeBetweenEachMessage();
            bus.Expect(b => b.Send(Arg<TrickleSmsWithDefinedTimeBetweenEachMessage>.Is.NotNull))
                .WhenCalled(i => trickleMessage = (TrickleSmsWithDefinedTimeBetweenEachMessage)((object[])(i.Arguments[0]))[0]);

            var service = new CoordinatorService { Bus = bus };
            var response = service.OnPost(request) as CoordinatorResponse;

            Assert.That(response.ResponseStatus.ErrorCode, Is.Null);
            Assert.That(response.RequestId, Is.Not.EqualTo(Guid.Empty));

            bus.VerifyAllExpectations();
        }
    }
}