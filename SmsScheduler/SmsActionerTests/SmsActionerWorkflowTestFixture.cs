using System;
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
    public class SmsActionerWorkflowTestFixture
    {
        // TODO: Add tests for data in messages being set

        [Test]
        public void SendSingleSmsNow_SuccessReturnedIsInvalid_HasNoDeliveryStatus()
        {
            var sendOneMessageNow = new SendOneMessageNow();

            var smsService = MockRepository.GenerateMock<ISmsService>();
            var smsSent = new SmsSent(new SmsConfirmationData("r", DateTime.Now, 0.44m));
            smsService.Expect(s => s.Send(sendOneMessageNow)).Return(smsSent);

            Test.Initialize();
            Assert.That(() => 
            Test.Saga<SmsActioner.SmsActioner>()
                .WithExternalDependencies(a => a.SmsService = smsService)
                    .ExpectPublish<MessageSent>()
                .When(a => a.Handle(sendOneMessageNow)), 
                Throws.Exception.With.Message.Contains("SmsSent type is invalid - followup is required to get delivery status")
            );
        }

        [Test]
        public void SendSingleSmsNow_QueuedReturnedIsInvalid_HasNoPrice()
        {
            var sendOneMessageNow = new SendOneMessageNow();

            var smsService = MockRepository.GenerateMock<ISmsService>();
            var smsQueued = new SmsQueued("sid");
            smsService.Expect(s => s.Send(sendOneMessageNow)).Return(smsQueued);

            Test.Initialize();
            Assert.That(() => 
            Test.Saga<SmsActioner.SmsActioner>()
                .WithExternalDependencies(a => a.SmsService = smsService)
                    .ExpectPublish<MessageSent>()
                .When(a => a.Handle(sendOneMessageNow)), 
                Throws.Exception.With.Message.Contains("SmsQueued type is invalid - followup is required to get delivery status")
            );
        }

        [Test]
        public void SendSingleSmsNow_Failure()
        {
            var sendOneMessageNow = new SendOneMessageNow();

            var smsService = MockRepository.GenerateMock<ISmsService>();
            var smsSent = new SmsFailed("sid", "code", "message");
            smsService.Expect(s => s.Send(sendOneMessageNow)).Return(smsSent);

            Test.Initialize();
            Test.Saga<SmsActioner.SmsActioner>()
                .WithExternalDependencies(a => a.SmsService = smsService)
                    .ExpectPublish<MessageFailedSending>()
                .When(a => a.Handle(sendOneMessageNow))
                .AssertSagaCompletionIs(true);
        }

        [Test]
        public void SendSingleSmsNow_SendingThenSuccess()
        {
            var sendOneMessageNow = new SendOneMessageNow();

            var smsService = MockRepository.GenerateMock<ISmsService>();

            var smsSending = new SmsSending("12", 0.06m);
            var smsSent = new SmsSent(new SmsConfirmationData("r", DateTime.Now, .44m));
            smsService.Expect(s => s.Send(sendOneMessageNow)).Return(smsSending);
            smsService.Expect(s => s.CheckStatus(smsSending.Sid)).Return(smsSent);

            Test.Initialize();
            Test.Saga<SmsActioner.SmsActioner>()
                .WithExternalDependencies(a => a.SmsService = smsService)
                    .ExpectTimeoutToBeSetIn<SmsPendingTimeout>((timeoutMessage, timespan) => timespan == TimeSpan.FromSeconds(10))
                .When(a => a.Handle(sendOneMessageNow))
                    .ExpectPublish<MessageSent>()
                .WhenSagaTimesOut()
                .AssertSagaCompletionIs(true);
        }

        [Test]
        public void SendSingleSmsNow_SendingThenFail()
        {
            var sendOneMessageNow = new SendOneMessageNow();

            var smsService = MockRepository.GenerateMock<ISmsService>();

            const string sid = "12";
            var smsSending = new SmsSending(sid, 0.06m);
            var smsFailed = new SmsFailed(sid, "c", "m");
            smsService.Expect(s => s.Send(sendOneMessageNow)).Return(smsSending);
            smsService.Expect(s => s.CheckStatus(smsSending.Sid)).Return(smsFailed);

            Test.Initialize();
            Test.Saga<SmsActioner.SmsActioner>()
                .WithExternalDependencies(a => a.SmsService = smsService)
                    .ExpectTimeoutToBeSetIn<SmsPendingTimeout>((timeoutMessage, timespan) => timespan == TimeSpan.FromSeconds(10))
                .When(a => a.Handle(sendOneMessageNow))
                    .ExpectNotPublish<MessageSent>()
                .WhenSagaTimesOut()
                .AssertSagaCompletionIs(true);
        }

        [Test]
        public void SendSingleSmsNow_SendingThenQueuedThenSuccess()
        {
            var sendOneMessageNow = new SendOneMessageNow();

            var smsService = MockRepository.GenerateMock<ISmsService>();

            const string sid = "12";
            var smsSending = new SmsSending(sid, 0.06m);
            var smsQueued = new SmsQueued(sid);
            var smsSuccess = new SmsSent(new SmsConfirmationData("r", DateTime.Now, 3.3m));
            smsService.Expect(s => s.Send(sendOneMessageNow)).Return(smsSending);
            smsService.Expect(s => s.CheckStatus(smsQueued.Sid)).Repeat.Once().Return(smsQueued);
            smsService.Expect(s => s.CheckStatus(smsQueued.Sid)).Return(smsSuccess);

            Test.Initialize();
            Test.Saga<SmsActioner.SmsActioner>()
                .WithExternalDependencies(a => a.SmsService = smsService)
                    .ExpectTimeoutToBeSetIn<SmsPendingTimeout>((timeoutMessage, timespan) => timespan == TimeSpan.FromSeconds(10))
                .When(a => a.Handle(sendOneMessageNow))
                    .ExpectNotPublish<MessageSent>()
                    .ExpectTimeoutToBeSetIn<SmsPendingTimeout>((timeoutMessage, timespan) => timespan == TimeSpan.FromSeconds(10))
                .WhenSagaTimesOut()
                    .ExpectPublish<MessageSent>()
                .WhenSagaTimesOut();
        }

        [Test]
        public void SmsSentUsesUtc()
        {
            var now = DateTime.Now;
            var smsConfirmationData = new SmsConfirmationData("receipt", now, 2m);
            Assert.That(smsConfirmationData.SentAtUtc.Hour, Is.EqualTo(now.Hour));
            Assert.That(smsConfirmationData.SentAtUtc.Minute, Is.EqualTo(now.Minute));
            Assert.That(smsConfirmationData.SentAtUtc.Second, Is.EqualTo(now.Second));
            Assert.That(smsConfirmationData.SentAtUtc.Kind, Is.Not.EqualTo(now.Kind));
            Assert.That(smsConfirmationData.SentAtUtc.Kind, Is.EqualTo(DateTimeKind.Utc));
        }
    }
}
