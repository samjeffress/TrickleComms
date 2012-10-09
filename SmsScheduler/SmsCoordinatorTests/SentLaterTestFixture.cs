using System;
using System.Collections.Generic;
using NServiceBus;
using NServiceBus.Testing;
using NUnit.Framework;
using Rhino.Mocks;
using SmsCoordinator;
using SmsMessages;
using SmsMessages.Commands;
using SmsMessages.CommonData;
using SmsMessages.Events;

namespace SmsCoordinatorTests
{
    [TestFixture]
    public class SentLaterTestFixture
    {
        [Test]
        public void ScheduleSmsForSendingLater()
        {
            var scheduleSmsForSendingLater = new ScheduleSmsForSendingLater { SendMessageAt = DateTime.Now.AddDays(1) };
            var sagaId = Guid.NewGuid();
            var messageSent = new MessageSent { CorrelationId = sagaId };

            var scheduledSmsData = new ScheduledSmsData 
            {
                Id = sagaId, 
                Originator = "place", 
                OriginalMessageId = "one",
                OriginalMessage = new ScheduleSmsForSendingLater { SmsData = new SmsData("1", "msg"), SmsMetaData = new SmsMetaData() }
            };

            Test.Initialize();
            Test.Saga<ScheduleSms>()
                .WithExternalDependencies(a => a.Data = scheduledSmsData)
                    .ExpectTimeoutToBeSetAt<ScheduleSmsTimeout>((state, timeout) => timeout == scheduleSmsForSendingLater.SendMessageAt)
                .When(s => s.Handle(scheduleSmsForSendingLater))
                    .ExpectSend<SendOneMessageNow>()
                .WhenSagaTimesOut()
                .When(s => s.Handle(messageSent))
                    .AssertSagaCompletionIs(true);
        }

        [Test]
        public void TimeoutPromptsMessageSending_Data()
        {
            var bus = MockRepository.GenerateMock<IBus>();

            var sendOneMessageNow = new SendOneMessageNow();
            bus.Expect(b => b.Send(Arg<SendOneMessageNow>.Is.NotNull))
                .WhenCalled(i => sendOneMessageNow = (SendOneMessageNow)((object[])(i.Arguments[0]))[0]);

            var dataId = Guid.NewGuid();
            var originalMessage = new ScheduleSmsForSendingLater { SmsData = new SmsData("3443", "message"), SmsMetaData = new SmsMetaData { Tags = new List<string> { "a", "b" }, Topic = "topic" } };
            var data = new ScheduledSmsData { Id = dataId, OriginalMessage = originalMessage};

            var scheduleSms = new ScheduleSms { Bus = bus, Data = data };
            var timeoutMessage = new ScheduleSmsTimeout();
            scheduleSms.Timeout(timeoutMessage);

            Assert.That(sendOneMessageNow.SmsData, Is.EqualTo(data.OriginalMessage.SmsData));
            Assert.That(sendOneMessageNow.SmsMetaData, Is.EqualTo(data.OriginalMessage.SmsMetaData));
            Assert.That(sendOneMessageNow.CorrelationId, Is.EqualTo(data.Id));

            bus.VerifyAllExpectations();
        }

        [Test]
        public void OriginalMessageGetsSavedToSaga_Data()
        {
            var bus = MockRepository.GenerateMock<IBus>();
            var data = new ScheduledSmsData();
            var originalMessage = new ScheduleSmsForSendingLater() { SendMessageAt = DateTime.Now };

            var scheduleSms = new ScheduleSms { Bus = bus, Data = data };
            scheduleSms.Handle(originalMessage);

            Assert.That(data.OriginalMessage, Is.EqualTo(originalMessage));
        }
    }
}