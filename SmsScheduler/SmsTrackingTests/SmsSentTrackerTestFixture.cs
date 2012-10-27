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
            var messageSent = new MessageSent { ConfirmationData = new SmsConfirmationData("receipt", DateTime.Now.AddMinutes(-10), 0.33m) };
            var smsSentAuditor = new SmsSentTracker { DocumentStore = DocumentStore };
            smsSentAuditor.Handle(messageSent);

            using (var session = DocumentStore.OpenSession())
            {
                var savedMessage = session.Load<MessageSent>(messageSent.ConfirmationData.Receipt);
                Assert.That(savedMessage, Is.Not.Null);
            }
        }

        [Test]
        public void HandleMessageSentWithConfirmationEmailSendsConfirmation()
        {
            var messageSent = new MessageSent 
            { 
                ConfirmationData = new SmsConfirmationData("receiptwithconfirmationemail", DateTime.Now.AddMinutes(-10), 0.33m),
                ConfirmationEmailAddress = "emailaddress"
            };
            
            var emailService = MockRepository.GenerateMock<IEmailService>();
            emailService.Expect(e => e.SendSmsSentConfirmation(messageSent));

            var smsSentAuditor = new SmsSentTracker { DocumentStore = DocumentStore, EmailService = emailService };
            smsSentAuditor.Handle(messageSent);

            using (var session = DocumentStore.OpenSession())
            {
                var savedMessage = session.Load<MessageSent>(messageSent.ConfirmationData.Receipt);
                Assert.That(savedMessage, Is.Not.Null);
            }

            emailService.VerifyAllExpectations();
        }
    }
}
