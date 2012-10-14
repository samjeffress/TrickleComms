using System;
using NUnit.Framework;
using SmsMessages.CommonData;
using SmsMessages.MessageSending;
using SmsTracking;

namespace SmsTrackingTests
{
    [TestFixture]
    public class SmsSentAuditorTestFixture : RavenTestBase
    {
        [Test]
        public void HandleMessageSent()
        {
            var messageSent = new MessageSent { ConfirmationData = new SmsConfirmationData("receipt", DateTime.Now.AddMinutes(-10), 0.33m) };
            var smsSentAuditor = new SmsSentTracker { DocumentStore = DocumentStore };
            smsSentAuditor.Handle(messageSent);

            using (var session = DocumentStore.OpenSession())
            {
                var savedMessage = session.Load<MessageSent>("receipt");
                Assert.That(savedMessage, Is.Not.Null);
            }
        }
    }
}
