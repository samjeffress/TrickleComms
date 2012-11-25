using System;
using System.Collections.Generic;
using NServiceBus;
using NUnit.Framework;
using Rhino.Mocks;
using SmsMessages.Coordinator;
using SmsTracking;
using SmsTrackingTests;
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
            var request = new Coordinator { Message = "msg", Numbers = new List<string> { "1" }, StartTime = DateTime.Now.AddDays(1), TimeSeparator = new TimeSpan(0,0,3)};
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
            Assert.That(response.Messages[0].Status, Is.EqualTo(MessageStatusTracking.Completed));
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
                    CurrentStatus = CoordinatorStatusTracking.Started,
                    MessageStatuses = new List<MessageSendingStatus>
                    {
                        new MessageSendingStatus
                        {
                            Number = "12313",
                            Status = MessageStatusTracking.Completed
                        },
                        new MessageSendingStatus
                        {
                            Number = "434039",
                            Status = MessageStatusTracking.Scheduled
                        }
                    }
                };
                session.Store(coordinatorTrackingData, _coordinatorId.ToString());
                session.SaveChanges();
            }
        }
    }
}