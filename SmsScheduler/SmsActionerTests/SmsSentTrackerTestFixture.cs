using System;
using NUnit.Framework;
using Rhino.Mocks;
using SmsActioner;
using SmsMessages.CommonData;
using SmsMessages.MessageSending.Events;
using SmsMessages.MessageSending.Responses;
using SmsTrackingModels;

namespace SmsActionerTests
{
    [TestFixture]
    public class SmsSentTrackerTestFixture : RavenTestBase
    {
        [Test]
        public void HandleMessageSentNoConfirmationEmail()
        {
            var messageSent = new MessageSuccessfullyDelivered { CorrelationId = Guid.NewGuid(), ConfirmationData = new SmsConfirmationData("receipt", DateTime.Now.AddMinutes(-10), 0.33m) };

            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            ravenDocStore.Expect(r => r.GetStore()).Return(DocumentStore);
            var smsSentAuditor = new SmsSentTracker { RavenStore = ravenDocStore };
            smsSentAuditor.Handle(messageSent);

            using (var session = DocumentStore.OpenSession())
            {
                var savedMessage = session.Load<SmsTrackingData>(messageSent.CorrelationId.ToString());
                Assert.That(savedMessage, Is.Not.Null);
            }
        }

        [Test]
        public void HandleMessageNotSentNoConfirmationEmail()
        {
            var messageSent = new MessageFailedSending
                {
                    CorrelationId = Guid.NewGuid(),
                    SmsFailed = new SmsFailed("232", "code", "bad", "no more", "fail"),
                };

            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            ravenDocStore.Expect(r => r.GetStore()).Return(DocumentStore);
            var smsSentAuditor = new SmsSentTracker { RavenStore = ravenDocStore };
            smsSentAuditor.Handle(messageSent);

            using (var session = DocumentStore.OpenSession())
            {
                var savedMessage = session.Load<SmsTrackingData>(messageSent.CorrelationId.ToString());
                Assert.That(savedMessage, Is.Not.Null);
            }
        }
    }
}