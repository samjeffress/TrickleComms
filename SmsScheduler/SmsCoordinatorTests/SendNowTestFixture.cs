using System;
using System.Collections.Generic;
using NServiceBus;
using NServiceBus.Testing;
using NUnit.Framework;
using Rhino.Mocks;
using Rhino.Mocks.Constraints;
using SmsCoordinator;
using SmsMessages;

namespace SmsCoordinatorTests
{
    [TestFixture]
    public class SendNowTestFixture
    {
        [Test]
        public void SendSinlgeSmsNow()
        {
            var sendOneMessageNow = new SendOneMessageNow();
            const string receipt = "receipt123";

            var smsService = MockRepository.GenerateMock<ISmsService>();
            smsService.Expect(s => s.Send(Arg<SendOneMessageNow>.Is.Anything)).Return(receipt);

            Test.Initialize();

            Test.Handler<SmsActioner>()
                .WithExternalDependencies(m => m.SmsService = smsService)
                .ExpectPublish<MessageSent>(null)
                .OnMessage<SendOneMessageNow>(s => s = sendOneMessageNow);

            smsService.VerifyAllExpectations();
        }

        [Test]
        public void SendSingleSmsNowData()
        {
            var sendOneMessageNow = new SendOneMessageNow
            {
                SmsData = new SmsData("0044044040", "message"),     
                SmsMetaData = new SmsMetaData { Topic = "MissedPayment", Tags = new List<string> { "Money", "Stuff" } }
            };
            var bus = MockRepository.GenerateStrictMock<IBus>();
            var smsService = MockRepository.GenerateMock<ISmsService>();

            smsService.Expect(s => s.Send(sendOneMessageNow)).Return("Receipt");
            var publishingMessage = MockRepository.GenerateStub<MessageSent>();
            bus.Expect(b => b.Publish(null as Action<MessageSent>))
                .Constraints(new PredicateConstraint<Action<MessageSent>>(c =>
                {
                    c.Invoke(publishingMessage);
                    return
                    publishingMessage.Receipt == "Receipt" &&
                    publishingMessage.SmsData == sendOneMessageNow.SmsData &&
                    publishingMessage.SmsMetaData == sendOneMessageNow.SmsMetaData &&
                    publishingMessage.CorrelationId == sendOneMessageNow.CorrelationId;
                }));

            var smsActioner = new SmsActioner { Bus = bus, SmsService = smsService };
            smsActioner.Handle(sendOneMessageNow);

            bus.VerifyAllExpectations();
            smsService.VerifyAllExpectations();
        }
    }
}
