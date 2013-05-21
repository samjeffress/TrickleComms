using System;
using System.Collections.Generic;
using NServiceBus;
using NUnit.Framework;
using Rhino.Mocks;
using SmsMessages.CommonData;
using SmsMessages.Coordinator.Commands;
using SmsTracking;
using SmsTrackingModels;
using SmsWeb.API;
using IRavenDocStore = SmsWeb.IRavenDocStore;

namespace SmsWebTests
{
    [TestFixture]
    public class ApiCoordinatorTestFixture : RavenTestBase
    {
        private Guid _coordinatorId = Guid.NewGuid();

        [Test]
        public void PostInvalidNeitherTimeSeparatorOrSendBySet()
        {
            var request = new Coordinator { Message = "msg", Numbers = new List<string> { "1" }, StartTimeUtc = DateTime.Now.AddMinutes(3) };
            var service = new CoordinatorService();
            var response = service.OnPost(request) as CoordinatorResponse;

            Assert.That(response.ResponseStatus.ErrorCode, Is.EqualTo("InvalidMessage"));
            Assert.That(response.ResponseStatus.Errors[0].Message, Is.EqualTo("Message must contain either Time Separator OR DateTime to send all messages by."));
        }

        [Test]
        public void PostInvalidBothTimeSeparatorAndSendBySet()
        {
            var request = new Coordinator { Message = "msg", Numbers = new List<string> { "1" }, StartTimeUtc = DateTime.Now.AddMinutes(3), SendAllByUtc = DateTime.Now.AddMinutes(33), TimeSeparator = new TimeSpan(300)};
            var service = new CoordinatorService();
            var response = service.OnPost(request) as CoordinatorResponse;

            Assert.That(response.ResponseStatus.ErrorCode, Is.EqualTo("InvalidMessage"));
            Assert.That(response.ResponseStatus.Errors[0].Message, Is.EqualTo("Message must contain either Time Separator OR DateTime to send all messages by."));
        }

        [Test]
        public void PostInvalidBothSendAllAtOnceAndSendBySet()
        {
            var request = new Coordinator { Message = "msg", Numbers = new List<string> { "1" }, StartTimeUtc = DateTime.Now.AddMinutes(3), SendAllByUtc = DateTime.Now.AddMinutes(33), SendAllAtOnce = true};
            var service = new CoordinatorService();
            var response = service.OnPost(request) as CoordinatorResponse;

            Assert.That(response.ResponseStatus.ErrorCode, Is.EqualTo("InvalidMessage"));
            Assert.That(response.ResponseStatus.Errors[0].Message, Is.EqualTo("Message must contain either Time Separator OR DateTime to send all messages by."));
        }

        [Test]
        public void PostInvalidBothTimeSeparatorAndSendAllAtOnceSet()
        {
            var request = new Coordinator { Message = "msg", Numbers = new List<string> { "1" }, StartTimeUtc = DateTime.Now.AddMinutes(3), SendAllAtOnce = true, TimeSeparator = new TimeSpan(300)};
            var service = new CoordinatorService();
            var response = service.OnPost(request) as CoordinatorResponse;

            Assert.That(response.ResponseStatus.ErrorCode, Is.EqualTo("InvalidMessage"));
            Assert.That(response.ResponseStatus.Errors[0].Message, Is.EqualTo("Message must contain either Time Separator OR DateTime to send all messages by."));
        }

        [Test]
        public void PostInvalidNoMessage()
        {
            var request = new Coordinator { Numbers = new List<string> { "1" }, StartTimeUtc = DateTime.Now.AddMinutes(1), SendAllByUtc = DateTime.Now.AddDays(1) };
            var service = new CoordinatorService();
            var response = service.OnPost(request) as CoordinatorResponse;

            Assert.That(response.ResponseStatus.ErrorCode, Is.EqualTo("InvalidMessage"));
            Assert.That(response.ResponseStatus.Errors[0].Message, Is.EqualTo("Sms Message Required"));
        }

        [Test]
        public void PostInvalidMessageExceedsLength()
        {
            var request = new Coordinator { Numbers = new List<string> { "1" }, StartTimeUtc = DateTime.Now.AddMinutes(1), SendAllByUtc = DateTime.Now.AddDays(1), Message = "sdf;lkj asd;vlkja sdvklja v;lka jsdvlka jsdvalsjk sdf;lkj asd;vlkja sdvklja v;lka jsdvlka jsdvalsjk sdf;lkj asd;vlkja sdvklja v;lka jsdvlka jsdvalsjk sdf;lkj asd;vlkja sdvklja v;lka jsdvlka jsdvalsjk sdf;lkj asd;vlkja sdvklja v;lka jsdvlka jsdvalsjk" };
            var service = new CoordinatorService();
            var response = service.OnPost(request) as CoordinatorResponse;

            Assert.That(response.ResponseStatus.ErrorCode, Is.EqualTo("InvalidMessage"));
            Assert.That(response.ResponseStatus.Errors[0].Message, Is.EqualTo("Sms exceeds 160 character length"));
        }

        [Test]
        public void PostInvalidNoNumbers()
        {
            var request = new Coordinator { Message = "msg", StartTimeUtc = DateTime.Now, SendAllByUtc = DateTime.Now.AddDays(1) };
            var service = new CoordinatorService();
            var response = service.OnPost(request) as CoordinatorResponse;

            Assert.That(response.ResponseStatus.ErrorCode, Is.EqualTo("InvalidMessage"));
            Assert.That(response.ResponseStatus.Errors[0].Message, Is.EqualTo("List of numbers required"));
        }

        [Test]
        public void PostInvalidNoStartTime()
        {
            var request = new Coordinator { Message = "msg", Numbers = new List<string> { "1" }, SendAllByUtc = DateTime.Now.AddDays(1) };
            var service = new CoordinatorService();
            var response = service.OnPost(request) as CoordinatorResponse;

            Assert.That(response.ResponseStatus.ErrorCode, Is.EqualTo("InvalidMessage"));
            Assert.That(response.ResponseStatus.Errors[0].Message, Is.EqualTo("Start time must be set"));
        }

        [Test]
        public void PostInvalidStartTimeInPast()
        {
            var request = new Coordinator { Message = "msg", Numbers = new List<string> { "1" }, StartTimeUtc = DateTime.Now.AddDays(-1), SendAllByUtc = DateTime.Now.AddDays(1) };
            var service = new CoordinatorService();
            var response = service.OnPost(request) as CoordinatorResponse;

            Assert.That(response.ResponseStatus.ErrorCode, Is.EqualTo("InvalidMessage"));
            Assert.That(response.ResponseStatus.Errors[0].Message, Is.EqualTo("Start time must not be in the past"));
        }

        [Test]
        public void PostNoTopic()
        {
            var request = new Coordinator { Message = "msg", Numbers = new List<string> { "1" }, StartTimeUtc = DateTime.Now.AddHours(1), SendAllByUtc = DateTime.Now.AddDays(1), Topic = string.Empty};
            var service = new CoordinatorService();
            var response = service.OnPost(request) as CoordinatorResponse;

            Assert.That(response.ResponseStatus.ErrorCode, Is.EqualTo("InvalidMessage"));
            Assert.That(response.ResponseStatus.Errors[0].Message, Is.EqualTo("Topic must be set"));
        }

        [Test]
        public void PostValidWithEndDateCoordinator()
        {
            var request = new Coordinator { Message = "msg", Numbers = new List<string> { "1" }, StartTimeUtc = DateTime.Now.AddDays(1), SendAllByUtc = DateTime.Now.AddDays(1), Topic = "topic"};

            var bus = MockRepository.GenerateMock<IBus>();
            var mapper = MockRepository.GenerateMock<ICoordinatorApiModelToMessageMapping>();

            mapper.Expect(m => m.MapToTrickleOverPeriod(Arg<Coordinator>.Is.Equal(request), Arg<Guid>.Is.Anything));
            bus.Expect(b => b.Send(Arg<TrickleSmsOverCalculatedIntervalsBetweenSetDates>.Is.NotNull));
            
            var service = new CoordinatorService { Bus = bus, Mapper = mapper };
            var response = service.OnPost(request) as CoordinatorResponse;

            Assert.That(response.ResponseStatus.ErrorCode, Is.Null);
            Assert.That(response.RequestId, Is.Not.EqualTo(Guid.Empty));

            bus.VerifyAllExpectations();
            mapper.VerifyAllExpectations();
        }

        [Test]
        public void PostValidTimeSeparatedCoordinator()
        {
            var request = new Coordinator { Message = "msg", Numbers = new List<string> { "1" }, StartTimeUtc = DateTime.Now.AddDays(1), TimeSeparator = new TimeSpan(0,0,3), Topic = "topic"};
            var bus = MockRepository.GenerateMock<IBus>();
            var mapper = MockRepository.GenerateMock<ICoordinatorApiModelToMessageMapping>();

            mapper.Expect(m => m.MapToTrickleSpacedByPeriod(Arg<Coordinator>.Is.Equal(request), Arg<Guid>.Is.Anything));
            bus.Expect(b => b.Send(Arg<TrickleSmsWithDefinedTimeBetweenEachMessage>.Is.NotNull));

            var service = new CoordinatorService { Bus = bus, Mapper = mapper };
            var response = service.OnPost(request) as CoordinatorResponse;

            Assert.That(response.ResponseStatus.ErrorCode, Is.Null);
            Assert.That(response.RequestId, Is.Not.EqualTo(Guid.Empty));

            bus.VerifyAllExpectations();
            mapper.VerifyAllExpectations();
        }

        [Test]
        public void PostValidSendAllAtOnceCoordinator()
        {
            var request = new Coordinator { Message = "msg", Numbers = new List<string> { "1" }, StartTimeUtc = DateTime.Now.AddDays(1), SendAllAtOnce = true, Topic = "topic"};
            var bus = MockRepository.GenerateMock<IBus>();
            var mapper = MockRepository.GenerateMock<ICoordinatorApiModelToMessageMapping>();

            mapper.Expect(m => m.MapToSendAllAtOnce(Arg<Coordinator>.Is.Equal(request), Arg<Guid>.Is.Anything));
            bus.Expect(b => b.Send(Arg<SendAllMessagesAtOnce>.Is.NotNull));

            var service = new CoordinatorService { Bus = bus, Mapper = mapper };
            var response = service.OnPost(request) as CoordinatorResponse;

            Assert.That(response.ResponseStatus.ErrorCode, Is.Null);
            Assert.That(response.RequestId, Is.Not.EqualTo(Guid.Empty));

            bus.VerifyAllExpectations();
            mapper.VerifyAllExpectations();
        }

        [Test]
        [Ignore("Raven doesn't like to dish out two stores / sessions for different databases per request - need to figure a work around (mocking probably)")]
        public void GetFound()
        {
            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            ravenDocStore.Expect(r => r.GetStore()).Return(base.DocumentStore);
            var service = new CoordinatorService {RavenDocStore = ravenDocStore};

            var request = new Coordinator { RequestId = _coordinatorId };
            var response = service.OnGet(request) as CoordinatorResponse;

            Assert.That(response.RequestId, Is.EqualTo(_coordinatorId));
            Assert.That(response.Messages.Count, Is.EqualTo(2));
            Assert.That(response.Messages[0].Number, Is.EqualTo("12313"));
            Assert.That(response.Messages[0].Status, Is.EqualTo(MessageStatusTracking.CompletedSuccess));
            Assert.That(response.Messages[1].Number, Is.EqualTo("434039"));
            Assert.That(response.Messages[1].Status, Is.EqualTo(MessageStatusTracking.Scheduled));

            ravenDocStore.VerifyAllExpectations();
        }

        [Test]
        public void GetNotFound()
        {
            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            ravenDocStore.Expect(r => r.GetStore()).Return(base.DocumentStore);
            var service = new CoordinatorService { RavenDocStore = ravenDocStore };

            var notFoundRequestId = Guid.NewGuid();
            var request = new Coordinator { RequestId = notFoundRequestId };
            var response = service.OnGet(request) as CoordinatorResponse;

            Assert.That(response.RequestId, Is.EqualTo(notFoundRequestId));
            Assert.That(response.ResponseStatus.ErrorCode, Is.EqualTo("NotFound"));
        }

        [SetUp]
        public void Setup()
        {
            using (var session = base.DocumentStore.OpenSession())
            {
                var coordinatorTrackingData = new CoordinatorTrackingData
                {
                    CoordinatorId = _coordinatorId,
                    CurrentStatus = CoordinatorStatusTracking.Started
                };
                var message1 = new ScheduleTrackingData
                                   {
                                       SmsData = new SmsData("12313", "message"),
                                       CoordinatorId = _coordinatorId,
                                       MessageStatus = MessageStatus.Sent
                                   };                
                var message2 = new ScheduleTrackingData
                                   {
                                       SmsData = new SmsData("434039", "message"),
                                       CoordinatorId = _coordinatorId,
                                       MessageStatus = MessageStatus.Scheduled
                                   };
                session.Store(coordinatorTrackingData, _coordinatorId.ToString());
                session.Store(message1, Guid.NewGuid().ToString());
                session.Store(message2, Guid.NewGuid().ToString());
                session.SaveChanges();
            }
        }
    }
}