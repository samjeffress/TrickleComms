using System;
using NUnit.Framework;
using Rhino.Mocks;
using SmsMessages.CommonData;
using SmsMessages.MessageSending;
using SmsTracking;

namespace SmsTrackingTests
{
    [TestFixture]
    public class SmsSentTrackerTestFixture : RavenTestBase
    {
        [Test]
        public void HandleMessageSentNoConfirmationEmail()
        {
            var messageSent = new MessageSent { CorrelationId = Guid.NewGuid(), ConfirmationData = new SmsConfirmationData("receipt", DateTime.Now.AddMinutes(-10), 0.33m) };

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
        public void HandleMessageSentWithConfirmationEmailSendsConfirmation()
        {
            var messageSent = new MessageSent 
            { 
                CorrelationId = Guid.NewGuid(),
                ConfirmationData = new SmsConfirmationData("receiptwithconfirmationemail", DateTime.Now.AddMinutes(-10), 0.33m),
                ConfirmationEmailAddress = "emailaddress"
            };
            
            var emailService = MockRepository.GenerateMock<IEmailService>();
            emailService.Expect(e => e.SendSmsSentConfirmation(messageSent));

            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            ravenDocStore.Expect(r => r.GetStore()).Return(DocumentStore);
            var smsSentAuditor = new SmsSentTracker { RavenStore = ravenDocStore, EmailService = emailService };
            smsSentAuditor.Handle(messageSent);

            using (var session = DocumentStore.OpenSession())
            {
                var savedMessage = session.Load<SmsTrackingData>(messageSent.CorrelationId.ToString());
                Assert.That(savedMessage, Is.Not.Null);
            }

            emailService.VerifyAllExpectations();
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

        [Test]
        public void HandleMessageNotSentWithEmailConfirmationGetsSent()
        {
            var messageFailedSending = new MessageFailedSending
            { 
                CorrelationId = Guid.NewGuid(),
                SmsFailed = new SmsFailed("232", "code", "bad", "no more", "fail"),
                ConfirmationEmailAddress = "emailaddress"
            };
            
            var emailService = MockRepository.GenerateMock<IEmailService>();
            emailService.Expect(e => e.SendSmsSentConfirmation(messageFailedSending));

            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            ravenDocStore.Expect(r => r.GetStore()).Return(DocumentStore);
            var smsSentAuditor = new SmsSentTracker { RavenStore = ravenDocStore, EmailService = emailService };
            smsSentAuditor.Handle(messageFailedSending);

            using (var session = DocumentStore.OpenSession())
            {
                var savedMessage = session.Load<SmsTrackingData>(messageFailedSending.CorrelationId.ToString());
                Assert.That(savedMessage, Is.Not.Null);
            }

            emailService.VerifyAllExpectations();
        }
    }
}
