using System;
using NServiceBus.Testing;
using NUnit.Framework;
using Rhino.Mocks;
using SmsCoordinator;
using SmsMessages.CommonData;
using SmsMessages.MessageSending;

namespace SmsCoordinatorTests
{
    [TestFixture]
    public class SmsActionerTestFixture
    {
        [Test]
        public void SendSingleSmsNow_Success()
        {
            var sendOneMessageNow = new SendOneMessageNow();

            var smsService = MockRepository.GenerateMock<ISmsService>();
            var smsSent = new SmsSent(new SmsConfirmationData("r", DateTime.Now, 0.44m));
            smsService.Expect(s => s.Send(sendOneMessageNow)).Return(smsSent);

            Test.Initialize();
            Test.Saga<SmsActioner>()
                .WithExternalDependencies(a => a.SmsService = smsService)
                    .ExpectPublish<MessageSent>()
                .When(a => a.Handle(sendOneMessageNow))
                .AssertSagaCompletionIs(true);
        }

        [Test]
        public void SendSingleSmsNow_Failure()
        {
            var sendOneMessageNow = new SendOneMessageNow();

            var smsService = MockRepository.GenerateMock<ISmsService>();
            var smsSent = new SmsFailed("sid", "code", "message", "moreinfo", "status");
            smsService.Expect(s => s.Send(sendOneMessageNow)).Return(smsSent);

            Test.Initialize();
            Test.Saga<SmsActioner>()
                .WithExternalDependencies(a => a.SmsService = smsService)
                    .ExpectPublish<MessageFailedSending>()
                .When(a => a.Handle(sendOneMessageNow))
                .AssertSagaCompletionIs(true);
        }

        [Test]
        public void SendSingleSmsNow_QueuedThenSuccess()
        {
            var sendOneMessageNow = new SendOneMessageNow();

            var smsService = MockRepository.GenerateMock<ISmsService>();

            var smsQueued = new SmsQueued("12");
            var smsSent = new SmsSent(new SmsConfirmationData("r", DateTime.Now, .44m));
            smsService.Expect(s => s.Send(sendOneMessageNow)).Return(smsQueued);
            smsService.Expect(s => s.Check(smsQueued.Sid)).Return(smsSent);

            Test.Initialize();
            Test.Saga<SmsActioner>()
                .WithExternalDependencies(a => a.SmsService = smsService)
                    .ExpectTimeoutToBeSetIn<SmsPendingTimeout>((timeoutMessage, timespan) => timespan == TimeSpan.FromMinutes(1))
                .When(a => a.Handle(sendOneMessageNow))
                    .ExpectPublish<MessageSent>(null)
                .WhenSagaTimesOut()
                .AssertSagaCompletionIs(true);
        }

        [Test]
        public void SendSingleSmsNow_QueuedThenFail()
        {
            var sendOneMessageNow = new SendOneMessageNow();

            var smsService = MockRepository.GenerateMock<ISmsService>();

            var sid = "12";
            var smsQueued = new SmsQueued(sid);
            var smsFailed = new SmsFailed(sid, "c", "m", "m", "s");
            smsService.Expect(s => s.Send(sendOneMessageNow)).Return(smsQueued);
            smsService.Expect(s => s.Check(smsQueued.Sid)).Return(smsFailed);

            Test.Initialize();
            Test.Saga<SmsActioner>()
                .WithExternalDependencies(a => a.SmsService = smsService)
                    .ExpectTimeoutToBeSetIn<SmsPendingTimeout>((timeoutMessage, timespan) => timespan == TimeSpan.FromMinutes(1))
                .When(a => a.Handle(sendOneMessageNow))
                    .ExpectNotPublish<MessageSent>(null)
                .WhenSagaTimesOut()
                .AssertSagaCompletionIs(true);
        }

        [Test]
        public void SendSingleSmsNow_QueuedTwiceThenSuccess()
        {
            var sendOneMessageNow = new SendOneMessageNow();

            var smsService = MockRepository.GenerateMock<ISmsService>();

            var sid = "12";
            var smsQueued = new SmsQueued(sid);
            var smsSuccess = new SmsSent(new SmsConfirmationData("r", DateTime.Now, 3.3m));
            smsService.Expect(s => s.Send(sendOneMessageNow)).Return(smsQueued);
            smsService.Expect(s => s.Check(smsQueued.Sid)).Repeat.Once().Return(smsQueued);
            smsService.Expect(s => s.Check(smsQueued.Sid)).Return(smsSuccess);

            Test.Initialize();
            Test.Saga<SmsActioner>()
                .WithExternalDependencies(a => a.SmsService = smsService)
                    .ExpectTimeoutToBeSetIn<SmsPendingTimeout>((timeoutMessage, timespan) => timespan == TimeSpan.FromMinutes(1))
                .When(a => a.Handle(sendOneMessageNow))
                    .ExpectNotPublish<MessageSent>(null)
                    .ExpectTimeoutToBeSetIn<SmsPendingTimeout>((timeoutMessage, timespan) => timespan == TimeSpan.FromMinutes(1))
                .WhenSagaTimesOut()
                    .ExpectPublish<MessageSent>(null)
                .WhenSagaTimesOut();
        }

        //[Test]
        //public void SendSinlgeSmsNow()
        //{
        //    var sendOneMessageNow = new SendOneMessageNow();

        //    var smsService = MockRepository.GenerateMock<ISmsService>();
        //    smsService.Expect(s => s.Send(Arg<SendOneMessageNow>.Is.Anything)).Return(new SmsConfirmationData("receipt", DateTime.Now, 2));

        //    Test.Initialize();

        //    Test.Handler<SmsActioner>()
        //        .WithExternalDependencies(m => m.SmsService = smsService)
        //        .ExpectPublish<MessageSent>(null)
        //        .OnMessage<SendOneMessageNow>(s => s = sendOneMessageNow);

        //    smsService.VerifyAllExpectations();
        //}

        //[Test]
        //public void SendSingleSmsNow_Data()
        //{
        //    var sendOneMessageNow = new SendOneMessageNow
        //    {
        //        SmsData = new SmsData("0044044040", "message"),     
        //        SmsMetaData = new SmsMetaData { Topic = "MissedPayment", Tags = new List<string> { "Money", "Stuff" } },
        //        ConfirmationEmailAddress = "blah",
        //        CorrelationId = Guid.NewGuid()
        //    };
        //    var bus = MockRepository.GenerateStrictMock<IBus>();
        //    var smsService = MockRepository.GenerateMock<ISmsService>();

        //    var smsConfirmationData = new SmsConfirmationData("Receipt", DateTime.Now, 2);
        //    smsService.Expect(s => s.Send(sendOneMessageNow)).Return(smsConfirmationData);
        //    var publishingMessage = MockRepository.GenerateStub<MessageSent>();
        //    bus.Expect(b => b.Publish(null as Action<MessageSent>))
        //        .Constraints(new PredicateConstraint<Action<MessageSent>>(c =>
        //        {
        //            c.Invoke(publishingMessage);
        //            return
        //            publishingMessage.ConfirmationData == smsConfirmationData &&
        //            publishingMessage.SmsData == sendOneMessageNow.SmsData &&
        //            publishingMessage.SmsMetaData == sendOneMessageNow.SmsMetaData &&
        //            publishingMessage.CorrelationId == sendOneMessageNow.CorrelationId && 
        //            publishingMessage.ConfirmationEmailAddress == sendOneMessageNow.ConfirmationEmailAddress;
        //        }));

        //    var smsActioner = new SmsActioner { Bus = bus, SmsService = smsService };
        //    smsActioner.Handle(sendOneMessageNow);

        //    bus.VerifyAllExpectations();
        //    smsService.VerifyAllExpectations();
        //}
    }
}
