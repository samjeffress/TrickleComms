using System;
using NServiceBus;
using NUnit.Framework;
using Raven.Client;
using Raven.Database.Server.Responders;
using Rhino.Mocks;
using SmsMessages.CommonData;
using SmsMessages.MessageSending.Commands;
using SmsTrackingModels;
using SmsTrackingModels.RavenIndexs;
using SmsWeb;
using SmsWeb.Controllers;

namespace SmsWebTests
{
    [TestFixture]
    public class RespondToIncomingSms
    {
        [Test]
        public void ValidForm_AcknowledgesIncomingSms_SendSmsPutOnBus()
        {
            var response = new RespondToSmsIncoming { IncomingSmsId = Guid.NewGuid()};
            var smsReceivedData = new SmsReceivedData{SmsData = new SmsData("mobile", "message"), SmsId = response.IncomingSmsId};

            var bus = MockRepository.GenerateMock<IBus>();
            var raven = MockRepository.GenerateMock<IRavenDocStore>();
            var docStore = MockRepository.GenerateMock<IDocumentStore>();
            var session = MockRepository.GenerateMock<IDocumentSession>();

            raven.Expect(d => d.GetStore()).Return(docStore);
            docStore.Expect(d => d.OpenSession()).Return(session);
            session.Expect(s => s.Load<SmsReceivedData>(response.IncomingSmsId.ToString())).Return(smsReceivedData);
            session.Expect(s => s.SaveChanges());
            // TODO : Mock the query action

            SendOneMessageNow sendMessageNow = null;
            bus.Expect(b => b.Send(Arg<SendOneMessageNow>.Is.Anything));
                // .WhenCalled(b => sendMessageNow = (SendOneMessageNow) b.Arguments[0]);

            var receivedMessageController = new ReceivedMessageController
            {
                Bus = bus,
                DocumentStore = raven
            };

            receivedMessageController.Respond(response);

            // Assert.That(sendMessageNow.SmsData.Mobile, Is.EqualTo(smsReceivedData.SmsData.Mobile));
            // Assert.That(sendMessageNow.SmsData.Message, Is.EqualTo(response.Message));
            // Assert.That(sendMessageNow.CorrelationId, Is.EqualTo(response.IncomingSmsId));
            Assert.That(smsReceivedData.Acknowledge, Is.True);
        }
    }
}