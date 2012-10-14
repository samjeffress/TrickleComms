using System;
using NUnit.Framework;
using Raven.Client;
using Raven.Client.Embedded;
using SmsMessages.CommonData;
using SmsMessages.Events;
using SmsTracking;

namespace SmsTrackingTests
{
    [TestFixture]
    public class SmsSentAuditorTestFixture
    {
        private IDocumentStore _documentStore;

        [SetUp]
        public void Setup()
        {
            _documentStore = new EmbeddableDocumentStore {DefaultDatabase = "SmsTracking"}.Initialize();
        }

        [TearDown]
        public void Teardown()
        {
            _documentStore.Dispose();
        }

        [Test]
        public void HandleMessageSent()
        {
            var messageSent = new MessageSent { ConfirmationData = new SmsConfirmationData("receipt", DateTime.Now.AddMinutes(-10), 0.33m) };
            var smsSentAuditor = new SmsSentAuditor { DocumentStore = _documentStore };
            smsSentAuditor.Handle(messageSent);

            using (var session = _documentStore.OpenSession())
            {
                var savedMessage = session.Load<MessageSent>("receipt");
                Assert.That(savedMessage, Is.Not.Null);
            }
        }
    }
}
