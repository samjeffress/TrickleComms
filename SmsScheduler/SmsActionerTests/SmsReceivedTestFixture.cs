using System;
using NServiceBus;
using NUnit.Framework;
using Raven.Client;
using Rhino.Mocks;
using Rhino.Mocks.Constraints;
using SmsActioner;
using SmsMessages.MessageSending.Events;

namespace SmsActionerTests
{
    [TestFixture]
    public class SmsReceivedTestFixture
    {
        [Test]
        public void MessageReceived()
        {
            var receivedMessage = new SmsReceieved();

            var bus = MockRepository.GenerateStrictMock<IBus>();
            var raven = MockRepository.GenerateMock<IRavenDocStore>();
            var docSession = MockRepository.GenerateMock<IDocumentSession>();

            bus.Expect(b => b.Publish(Arg<Action<MessageReceived>>.Is.Anything));
            raven.Expect(r => r.GetStore().OpenSession()).Return(docSession);
            docSession.Expect(d => d.Store(receivedMessage));
            docSession.Expect(d => d.SaveChanges());

            var smsReceivedHandler = new SmsReceivedHandler { Bus = bus, RavenDocStore = raven };
            smsReceivedHandler.Get(receivedMessage);

            bus.VerifyAllExpectations();
            raven.VerifyAllExpectations();
            docSession.VerifyAllExpectations();
        }
    }
}