using System;
using System.Collections.Generic;
using NServiceBus;
using NUnit.Framework;
using Rhino.Mocks;
using SmsMessages.Coordinator.Events;
using SmsTracking;
using SmsTrackingMessages.Messages;
using SmsTrackingModels;

namespace SmsTrackingTests
{
    [TestFixture]
    public class SmsCoordinatorTestFixture : RavenTestBase
    {
        [Test]
        public void CoordinateMessagesCompleteWithAllMessagesCompleteNoConfirmationEmailAddressesStillSendsConfirmationEmail()
        {
            var coordinatorId = Guid.NewGuid();

            using (var session = DocumentStore.OpenSession())
            {
                var messageStatuses = new List<MessageSendingStatus> { new MessageSendingStatus { Number = "2323", ScheduledSendingTimeUtc = DateTime.Now, ActualSentTimeUtc = DateTime.Now, Cost = 0.33m, Status = MessageStatusTracking.CompletedSuccess } };
                var coordinatorTrackingData = new CoordinatorTrackingData(messageStatuses)
                {
                    CoordinatorId = coordinatorId,
                    CurrentStatus = CoordinatorStatusTracking.Completed
                };
                session.Store(coordinatorTrackingData, coordinatorId.ToString());
                session.SaveChanges();
            }

            var coordinatorCompleted = new CoordinatorCompleted { CoordinatorId = coordinatorId };
            
            var bus = MockRepository.GenerateMock<IBus>();
            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            ravenDocStore.Expect(r => r.GetStore()).Return(DocumentStore);
            CoordinatorCompleteEmail message = null;
            bus.Expect(b => b.Send(Arg<CoordinatorCompleteEmail>.Is.Anything))
                .WhenCalled(a => message = (CoordinatorCompleteEmail)(((Object[])(a.Arguments[0]))[0])); ;

            var coordinatorTracker = new CoordinatorTracker() { RavenStore = ravenDocStore, Bus = bus };
            coordinatorTracker.Handle(coordinatorCompleted);

            using (var session = DocumentStore.OpenSession())
            {
                var trackingData = session.Load<CoordinatorTrackingData>(coordinatorId.ToString());
                Assert.That(trackingData.CurrentStatus, Is.EqualTo(CoordinatorStatusTracking.Completed));
            }

            Assert.That(message.EmailAddresses.Count, Is.EqualTo(0));

            bus.VerifyAllExpectations();
            ravenDocStore.VerifyAllExpectations();
        }

        [Test]
        public void CoordinateMessagesCompleteWithAllMessagesCompleteAndSendsConfirmationEmail()
        {
            var coordinatorId = Guid.NewGuid();

            using (var session = DocumentStore.OpenSession())
            {
                var messageStatuses = new List<MessageSendingStatus>
                {
                    new MessageSendingStatus { Number = "2323", ScheduledSendingTimeUtc = DateTime.Now, ActualSentTimeUtc = DateTime.Now, Cost = 0.33m, Status = MessageStatusTracking.CompletedSuccess },
                    new MessageSendingStatus { Number = "2324", ScheduledSendingTimeUtc = DateTime.Now, Status = MessageStatusTracking.CompletedFailure, FailureData = new FailureData() }
                };
                var coordinatorTrackingData = new CoordinatorTrackingData(messageStatuses)
                {
                    CoordinatorId = coordinatorId,
                    ConfirmationEmailAddress = "email",
                    CurrentStatus = CoordinatorStatusTracking.Completed
                };
                session.Store(coordinatorTrackingData, coordinatorId.ToString());
                session.SaveChanges();
            }

            var coordinatorCompleted = new CoordinatorCompleted { CoordinatorId = coordinatorId };

            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            var bus = MockRepository.GenerateMock<IBus>();
            ravenDocStore.Expect(r => r.GetStore()).Return(DocumentStore);
            CoordinatorCompleteEmail message = null;
            bus.Expect(b => b.Send(Arg<CoordinatorCompleteEmail>.Is.Anything)).WhenCalled(a => message = (CoordinatorCompleteEmail)(((Object[])(a.Arguments[0]))[0])); ;

            var coordinatorTracker = new CoordinatorTracker { RavenStore = ravenDocStore, Bus = bus};
            coordinatorTracker.Handle(coordinatorCompleted);

            using (var session = DocumentStore.OpenSession())
            {
                var trackingData = session.Load<CoordinatorTrackingData>(coordinatorId.ToString());
                Assert.That(trackingData.CurrentStatus, Is.EqualTo(CoordinatorStatusTracking.Completed));
            }

            Assert.That(message.EmailAddresses.Count, Is.EqualTo(1));
            bus.VerifyAllExpectations();
        }
    }
}