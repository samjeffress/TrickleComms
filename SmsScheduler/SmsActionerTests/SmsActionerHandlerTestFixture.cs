using System;
using System.Collections.Generic;
using NServiceBus;
using NServiceBus.Testing;
using NUnit.Framework;
using Rhino.Mocks;
using SmsActioner;
using SmsMessages.CommonData;
using SmsMessages.MessageSending.Commands;
using SmsMessages.MessageSending.Events;

namespace SmsActionerTests
{
    [TestFixture]
    public class SmsActionerHandlerTestFixture
    {
        [Test]
        public void SendOneMessageNow_SmsSent_InvalidThrowsException()
        {
            var sendOneMessageNow = new SendOneMessageNow();

            var smsService = MockRepository.GenerateMock<ISmsService>();
            var smsSent = new SmsSent(new SmsConfirmationData("r", DateTime.Now, 0.06m));

            smsService
                .Expect(s => s.Send(sendOneMessageNow))
                .Return(smsSent);

            var smsActioner = new SmsActioner.SmsActioner
                {
                    SmsService = smsService
                };

            Assert.That(() => smsActioner.Handle(sendOneMessageNow), Throws.Exception.With.Message.Contains("SmsSent type is invalid - followup is required to get delivery status"));
        }

        [Test]
        public void SendOneMessageNow_SmsQueued_InvalidThrowsException()
        {
            var sendOneMessageNow = new SendOneMessageNow();

            var smsService = MockRepository.GenerateMock<ISmsService>();
            var smsQueued = new SmsQueued("sid");

            smsService
                .Expect(s => s.Send(sendOneMessageNow))
                .Return(smsQueued);

            var smsActioner = new SmsActioner.SmsActioner
                {
                    SmsService = smsService
                };

            Assert.That(() => smsActioner.Handle(sendOneMessageNow), Throws.Exception.With.Message.Contains("SmsQueued type is invalid - followup is required to get delivery status"));
        }

        [Test]
        public void SendOneMessageNow_SmsSendingSetsPriceAndId()
        {
            var sendOneMessageNow = new SendOneMessageNow();

            var smsService = MockRepository.GenerateMock<ISmsService>();
            var smsSending = new SmsSending("id", 0.06m);

            smsService
                .Expect(s => s.Send(sendOneMessageNow))
                .Return(smsSending);
            var data = new SmsActionerData();

            Test.Initialize();
            Test.Saga<SmsActioner.SmsActioner>()
                .WithExternalDependencies(a =>
                    {
                        a.SmsService = smsService;
                        a.Data = data;
                    })
                .WhenReceivesMessageFrom("somewhere")
                    .ExpectTimeoutToBeSetIn<SmsPendingTimeout>((timeoutMessage, timespan) => timespan == TimeSpan.FromSeconds(10))
                .When(a => a.Handle(sendOneMessageNow));

            Assert.That(data.SmsRequestId, Is.EqualTo(smsSending.Sid));
            Assert.That(data.Price, Is.EqualTo(smsSending.Price));
            Assert.That(data.OriginalMessage, Is.EqualTo(sendOneMessageNow));
            smsService.VerifyAllExpectations();
        }

        [Test]
        public void SendOneMessageNow_SmsFailed()
        {
            var sendOneMessageNow = new SendOneMessageNow
                {
                    ConfirmationEmailAddress = "abc@def.com",
                    CorrelationId = Guid.NewGuid(),
                    SmsData = new SmsData("mobile", "message"),
                    SmsMetaData = new SmsMetaData { Tags = new List<string> { "tag1", "tag2" }, Topic = "topic" }
                };

            var smsService = MockRepository.GenerateMock<ISmsService>();
            var smsFailed = new SmsFailed("sid", "faile", "why why why");

            smsService
                .Expect(s => s.Send(sendOneMessageNow))
                .Return(smsFailed);
            var data = new SmsActionerData();

            Test.Initialize();
            Test.Saga<SmsActioner.SmsActioner>()
                .WithExternalDependencies(a =>
                    {
                        a.SmsService = smsService;
                        a.Data = data;
                    })
                .WhenReceivesMessageFrom("somewhere")
                    .ExpectPublish<MessageFailedSending>(message => 
                        message.ConfirmationEmailAddress == sendOneMessageNow.ConfirmationEmailAddress &&
                        message.CorrelationId == sendOneMessageNow.CorrelationId &&
                        message.SmsData == sendOneMessageNow.SmsData && 
                        message.SmsMetaData == sendOneMessageNow.SmsMetaData &&
                        message.SmsFailed == smsFailed
                    )
                .When(a => a.Handle(sendOneMessageNow))
                .AssertSagaCompletionIs(true);

            smsService.VerifyAllExpectations();
        }
    }
}