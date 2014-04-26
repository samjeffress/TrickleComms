using System;
using NServiceBus;
using NServiceBus.Testing;
using NUnit.Framework;
using Rhino.Mocks;
using SmsMessages;
using SmsMessages.CommonData;
using SmsMessages.MessageSending.Commands;
using SmsMessages.MessageSending.Responses;
using SmsMessages.Scheduling.Commands;
using SmsMessages.Scheduling.Events;
using SmsMessages.Tracking.Scheduling.Commands;
using SmsScheduler;

namespace SmsSchedulerTests
{
    [TestFixture]
    public class EmailSchedulingTestFixture
    {
        [Test]
        public void ScheduleEmailForSendingLater()
        {
            var scheduleEmailForSendingLater = new ScheduleEmailForSendingLater
                {
                    SendMessageAtUtc = DateTime.Now.AddDays(1),
                    ScheduleMessageId = Guid.NewGuid(),
                    EmailData = new EmailData()
                };
            var sagaId = Guid.NewGuid();

            var scheduledEmailData = new ScheduledEmailData
                {
                    Id = sagaId,
                    Originator = "place",
                    OriginalMessageId = Guid.NewGuid().ToString(),
                    OriginalMessageData = new OriginalEmailMessageData(new ScheduleEmailForSendingLater { EmailData = scheduleEmailForSendingLater.EmailData})
                };

            Test.Initialize();
            Test.Saga<EmailScheduler>()
                .WithExternalDependencies(a => { a.Data = scheduledEmailData; })
                .ExpectTimeoutToBeSetAt<ScheduleEmailTimeout>((state, timeout) => timeout == scheduleEmailForSendingLater.SendMessageAtUtc)
                .ExpectSendLocal<EmailScheduleCreated>()
                .When(s => s.Handle(scheduleEmailForSendingLater))
                .ExpectSend<SendOneEmailNow>()
                .WhenSagaTimesOut()
                .ExpectPublish<ScheduledEmailSent>()
                .ExpectSendLocal<ScheduleStatusChanged>(s => s.Status == MessageStatus.Sent)
                .When(s => s.Handle(new EmailStatusUpdate { Status = EmailStatus.Opened }))
                .AssertSagaCompletionIs(true);
        }

        [Test]
        public void ScheduleEmailForSendingLaterFails()
        {
            var scheduleEmailForSendingLater = new ScheduleEmailForSendingLater
                {
                    SendMessageAtUtc = DateTime.Now.AddDays(1),
                    ScheduleMessageId = Guid.NewGuid(),
                    EmailData = new EmailData()
                };
            var sagaId = Guid.NewGuid();
            var scheduledEmailData = new ScheduledEmailData
                {
                    Id = sagaId,
                    Originator = "place",
                    OriginalMessageId = Guid.NewGuid().ToString(),
                    OriginalMessageData = new OriginalEmailMessageData(new ScheduleEmailForSendingLater { EmailData = scheduleEmailForSendingLater.EmailData })
                };

            Test.Initialize();
            Test.Saga<EmailScheduler>()
                .WithExternalDependencies(a => { a.Data = scheduledEmailData; })
                .ExpectTimeoutToBeSetAt<ScheduleEmailTimeout>((state, timeout) => timeout == scheduleEmailForSendingLater.SendMessageAtUtc)
                .When(s => s.Handle(scheduleEmailForSendingLater))
                .ExpectSend<SendOneEmailNow>()
                .WhenSagaTimesOut()
                .ExpectPublish<ScheduledEmailFailed>()
                .ExpectSendLocal<ScheduleStatusChanged>(s => s.Status == MessageStatus.Failed)
                .When(s => s.Handle(new EmailStatusUpdate { Status = EmailStatus.Failed }))
                .AssertSagaCompletionIs(true);
        }

        [Test]
        public void ScheduleEmailForSendingLaterIsPaused()
        {
            var scheduleEmailForSendingLater = new ScheduleEmailForSendingLater
                {
                    SendMessageAtUtc = DateTime.Now.AddDays(1),
                    ScheduleMessageId = Guid.NewGuid(),
                    EmailData = new EmailData()
                };
            var sagaId = Guid.NewGuid();

            var scheduledEmailData = new ScheduledEmailData
                {
                    Id = sagaId,
                    Originator = "place",
                    OriginalMessageId = "one",
                    OriginalMessageData = new OriginalEmailMessageData(new ScheduleEmailForSendingLater { EmailData = scheduleEmailForSendingLater.EmailData })
                };

            Test.Initialize();
            Test.Saga<EmailScheduler>()
                .WithExternalDependencies(a => { a.Data = scheduledEmailData; })
                .ExpectTimeoutToBeSetAt<ScheduleEmailTimeout>((state, timeout) => timeout == scheduleEmailForSendingLater.SendMessageAtUtc)
                .ExpectSendLocal<EmailScheduleCreated>()
                .When(s => s.Handle(scheduleEmailForSendingLater))
                .ExpectPublish<MessageSchedulePaused>()
                .ExpectSendLocal<ScheduleStatusChanged>(s => s.Status == MessageStatus.Paused)
                .When(s => s.Handle(new PauseScheduledMessageIndefinitely(Guid.Empty)))
                .ExpectNotSend<SendOneEmailNow>(now => false)
                .WhenSagaTimesOut();
        }

        [Test]
        public void ScheduleEmailForSendingLaterIsPausedThenResumedAndSent()
        {
            var scheduleEmailForSendingLater = new ScheduleEmailForSendingLater
                {
                    SendMessageAtUtc = DateTime.Now.AddDays(1),
                    EmailData = new EmailData()
                };
            var sagaId = Guid.NewGuid();

            var scheduledEmailData = new ScheduledEmailData
                {
                    Id = sagaId,
                    Originator = "place",
                    OriginalMessageId = Guid.NewGuid().ToString(),
                    OriginalMessageData = new OriginalEmailMessageData(new ScheduleEmailForSendingLater { EmailData= scheduleEmailForSendingLater.EmailData })
                };

            Test.Initialize();
            Test.Saga<EmailScheduler>()
                .WithExternalDependencies(a => { a.Data = scheduledEmailData; })
                .ExpectTimeoutToBeSetAt<ScheduleEmailTimeout>((state, timeout) => timeout == scheduleEmailForSendingLater.SendMessageAtUtc && state.TimeoutCounter == 0)
                .ExpectSendLocal<EmailScheduleCreated>()
                .When(s => s.Handle(scheduleEmailForSendingLater))
                .ExpectPublish<MessageSchedulePaused>()
                .ExpectSendLocal<ScheduleStatusChanged>(s => s.Status == MessageStatus.Paused)
                .When(s => s.Handle(new PauseScheduledMessageIndefinitely(Guid.Empty)))
                .ExpectTimeoutToBeSetAt<ScheduleEmailTimeout>((state, timeout) => state.TimeoutCounter == 1)
                .ExpectPublish<MessageRescheduled>()
                .ExpectSendLocal<ScheduleStatusChanged>(s => s.Status == MessageStatus.Scheduled)
                .When(s => s.Handle(new ResumeScheduledMessageWithOffset(Guid.Empty, new TimeSpan())))
                .ExpectNotSend<SendOneEmailNow>(now => false)
                .When(s => s.Timeout(new ScheduleEmailTimeout { TimeoutCounter = 0 }))
                .ExpectSend<SendOneEmailNow>()
                .When(s => s.Timeout(new ScheduleEmailTimeout { TimeoutCounter = 1 }))
                .ExpectPublish<ScheduledEmailSent>()
                .ExpectSendLocal<ScheduleStatusChanged>(s => s.Status == MessageStatus.Sent)
                .When(s => s.Handle(new EmailStatusUpdate { Status = EmailStatus.Opened}))
                .AssertSagaCompletionIs(true);
        }

        [Test]
        public void ScheduleEmailForSendingLaterIsPausedThenRescheduledAndSent()
        {
            var scheduleEmailForSendingLater = new ScheduleEmailForSendingLater
                {
                    SendMessageAtUtc = DateTime.Now.AddDays(1),
                    ScheduleMessageId = Guid.NewGuid(),
                    EmailData = new EmailData()
                };
            var sagaId = Guid.NewGuid();

            var scheduledEmailData = new ScheduledEmailData
                {
                    Id = sagaId,
                    Originator = "place",
                    OriginalMessageId = Guid.NewGuid().ToString(),
                    OriginalMessageData = new OriginalEmailMessageData(new ScheduleEmailForSendingLater { EmailData = scheduleEmailForSendingLater.EmailData })
                };

            Test.Initialize();
            Test.Saga<EmailScheduler>()
                .WithExternalDependencies(a => { a.Data = scheduledEmailData; })
                .ExpectTimeoutToBeSetAt<ScheduleEmailTimeout>((state, timeout) => timeout == scheduleEmailForSendingLater.SendMessageAtUtc && state.TimeoutCounter == 0)
                .ExpectSendLocal<EmailScheduleCreated>()
                .When(s => s.Handle(scheduleEmailForSendingLater))
                .ExpectPublish<MessageSchedulePaused>()
                .ExpectSendLocal<ScheduleStatusChanged>(s => s.Status == MessageStatus.Paused)
                .When(s => s.Handle(new PauseScheduledMessageIndefinitely(Guid.Empty)))
                .ExpectTimeoutToBeSetAt<ScheduleEmailTimeout>((state, timeout) => state.TimeoutCounter == 1)
                .ExpectPublish<MessageRescheduled>()
                .ExpectSendLocal<ScheduleStatusChanged>(s => s.Status == MessageStatus.Scheduled)
                .When(s => s.Handle(new RescheduleScheduledMessageWithNewTime(Guid.Empty, new DateTime(2040, 4, 4, 4, 4, 4, DateTimeKind.Utc))))
                .ExpectNotSend<SendOneEmailNow>(now => false)
                .When(s => s.Timeout(new ScheduleEmailTimeout { TimeoutCounter = 0 }))
                .ExpectSend<SendOneEmailNow>()
                .When(s => s.Timeout(new ScheduleEmailTimeout { TimeoutCounter = 1 }))
                .ExpectPublish<ScheduledEmailSent>()
                .ExpectSendLocal<ScheduleStatusChanged>(s => s.Status == MessageStatus.Sent)
                .When(s => s.Handle(new EmailStatusUpdate { Status = EmailStatus.Opened }))
                .AssertSagaCompletionIs(true);
        }

        [Test]
        public void ScheduleEmailForSendingLaterButIsPausedThenResumedOutOfOrderAndSent()
        {
            var scheduleEmailForSendingLater = new ScheduleEmailForSendingLater
                {
                    SendMessageAtUtc = DateTime.Now.AddDays(1),
                    ScheduleMessageId = Guid.NewGuid(),
                    EmailData = new EmailData()
                };
            var sagaId = Guid.NewGuid();

            var scheduledEmailData = new ScheduledEmailData
                {
                    Id = sagaId,
                    Originator = "place",
                    OriginalMessageId = Guid.NewGuid().ToString(),
                    OriginalMessageData = new OriginalEmailMessageData(new ScheduleEmailForSendingLater { EmailData = scheduleEmailForSendingLater.EmailData })
                };

            Test.Initialize();
            Test.Saga<EmailScheduler>()
                .WithExternalDependencies(a => { a.Data = scheduledEmailData; })
                .ExpectTimeoutToBeSetAt<ScheduleEmailTimeout>((state, timeout) => timeout == scheduleEmailForSendingLater.SendMessageAtUtc && state.TimeoutCounter == 0)
                .ExpectSendLocal<EmailScheduleCreated>()
                .When(s => s.Handle(scheduleEmailForSendingLater))
                .ExpectTimeoutToBeSetAt<ScheduleEmailTimeout>((state, timeout) => state.TimeoutCounter == 1)
                .ExpectPublish<MessageRescheduled>()
                .ExpectSendLocal<ScheduleStatusChanged>(s => s.Status == MessageStatus.Scheduled)
                .When(s => s.Handle(new ResumeScheduledMessageWithOffset(Guid.Empty, new TimeSpan()) { MessageRequestTimeUtc = DateTime.Now }))
                .When(s => s.Handle(new PauseScheduledMessageIndefinitely(Guid.Empty) { MessageRequestTimeUtc = DateTime.Now.AddMinutes(-10) }))
                .ExpectSend<SendOneEmailNow>()
                .When(s => s.Timeout(new ScheduleEmailTimeout { TimeoutCounter = 1 }))
                .ExpectPublish<ScheduledEmailSent>()
                .ExpectSendLocal<ScheduleStatusChanged>(s => s.Status == MessageStatus.Sent)
                .When(s => s.Handle(new EmailStatusUpdate { Status = EmailStatus.Opened }))
                .AssertSagaCompletionIs(true);
        }

        [Test]
        public void ResumePausedSchedule_Data()
        {
            var sagaId = Guid.NewGuid();
            var scheduleMessageId = Guid.NewGuid();

            var scheduledEmailData = new ScheduledEmailData
                {
                    Id = sagaId,
                    ScheduleMessageId = scheduleMessageId,
                    Originator = "place",
                    OriginalMessageId = Guid.NewGuid().ToString(),
                    OriginalMessageData = new OriginalEmailMessageData(new ScheduleEmailForSendingLater { EmailData = new EmailData()}) {OriginalRequestSendTime = DateTime.Now }
                };

            Test.Initialize();
            var rescheduleMessage = new ResumeScheduledMessageWithOffset(scheduleMessageId, new TimeSpan(0, 1, 0, 0));
            var rescheduledTime = scheduledEmailData.OriginalMessageData.OriginalRequestSendTime.Add(rescheduleMessage.Offset);
            Test.Saga<EmailScheduler>()
                .WithExternalDependencies(a => { a.Data = scheduledEmailData; })
                .ExpectTimeoutToBeSetAt<ScheduleEmailTimeout>((state, span) => span == rescheduledTime)
                .ExpectPublish<MessageRescheduled>()
                .When(s => s.Handle(rescheduleMessage));
        }

        [Test]
        public void ReschedulePausedSchedule_Data()
        {
            var sagaId = Guid.NewGuid();
            var scheduleMessageId = Guid.NewGuid();

            var scheduledEmailData = new ScheduledEmailData
                {
                    Id = sagaId,
                    ScheduleMessageId = scheduleMessageId,
                    Originator = "place",
                    OriginalMessageId = Guid.NewGuid().ToString(),
                    OriginalMessageData = new OriginalEmailMessageData(new ScheduleEmailForSendingLater { EmailData = new EmailData(), SendMessageAtUtc = DateTime.Now })
                };

            Test.Initialize();
            var rescheduleMessage = new RescheduleScheduledMessageWithNewTime(scheduleMessageId, new DateTime(2040, 4, 4, 4, 4, 4, DateTimeKind.Utc));
            Test.Saga<EmailScheduler>()
                .WithExternalDependencies(a => { a.Data = scheduledEmailData; })
                .ExpectTimeoutToBeSetAt<ScheduleEmailTimeout>((state, span) => span == rescheduleMessage.NewScheduleTimeUtc)
                .ExpectPublish<MessageRescheduled>()
                .When(s => s.Handle(rescheduleMessage));

            Assert.IsFalse(scheduledEmailData.SchedulingPaused);
        }

        [Test]
        public void TimeoutPromptsMessageSending_Data()
        {
            var bus = MockRepository.GenerateMock<IBus>();

            var sendOneEmailNow = new SendOneEmailNow();
            bus.Expect(b => b.Send(Arg<SendOneEmailNow>.Is.NotNull))
               .WhenCalled(i => sendOneEmailNow = (SendOneEmailNow)(i.Arguments[0]));

            var dataId = Guid.NewGuid();
            var originalMessage = new ScheduleEmailForSendingLater { EmailData = new EmailData
                {
                    BodyHtml = "html",
                    BodyText = "text",
                    FromAddress = "from",
                    FromDisplayName = "display",
                    ReplyToAddress = "replyto",
                    Subject = "subject",
                    ToAddress = "to"
                }};
            var data = new ScheduledEmailData { Id = dataId, OriginalMessageData = new OriginalEmailMessageData(originalMessage) };

            var scheduleEmail = new EmailScheduler { Bus = bus, Data = data };
            var timeoutMessage = new ScheduleEmailTimeout();
            scheduleEmail.Timeout(timeoutMessage);

            Assert.That(sendOneEmailNow.BodyHtml, Is.EqualTo(data.OriginalMessageData.EmailData.BodyHtml));
            Assert.That(sendOneEmailNow.BodyText, Is.EqualTo(data.OriginalMessageData.EmailData.BodyText));
            Assert.That(sendOneEmailNow.FromAddress, Is.EqualTo(data.OriginalMessageData.EmailData.FromAddress));
            Assert.That(sendOneEmailNow.FromDisplayName, Is.EqualTo(data.OriginalMessageData.EmailData.FromDisplayName));
            Assert.That(sendOneEmailNow.ReplyToAddress, Is.EqualTo(data.OriginalMessageData.EmailData.ReplyToAddress));
            Assert.That(sendOneEmailNow.Subject, Is.EqualTo(data.OriginalMessageData.EmailData.Subject));
            Assert.That(sendOneEmailNow.ToAddress, Is.EqualTo(data.OriginalMessageData.EmailData.ToAddress));
            Assert.That(sendOneEmailNow.Username, Is.EqualTo(data.OriginalMessageData.Username));
            Assert.That(sendOneEmailNow.CorrelationId, Is.EqualTo(data.Id));

            bus.VerifyAllExpectations();
        }

        [Test]
        public void TimeoutSendingPausedNoAction_Data()
        {
            var bus = MockRepository.GenerateStrictMock<IBus>();

            var dataId = Guid.NewGuid();
            var originalMessage = new ScheduleEmailForSendingLater { EmailData = new EmailData()};
            var data = new ScheduledEmailData { Id = dataId, OriginalMessageData = new OriginalEmailMessageData(originalMessage), SchedulingPaused = true };

            var scheduleEmail = new EmailScheduler { Bus = bus, Data = data };
            var timeoutMessage = new ScheduleEmailTimeout();
            scheduleEmail.Timeout(timeoutMessage);

            bus.VerifyAllExpectations();
        }

        [Test]
        [Ignore("Do we really need to test this?")]
        public void OriginalMessageGetsSavedToSaga_Data()
        {
            var data = new ScheduledEmailData();
            var originalMessage = new ScheduleEmailForSendingLater { SendMessageAtUtc = DateTime.Now };

            Test.Initialize();
            Test.Saga<EmailScheduler>()
                .WithExternalDependencies(a => { a.Data = data; })
                .WhenReceivesMessageFrom("address")
                .ExpectPublish<SmsScheduled>(m => m.CoordinatorId == data.Id && m.ScheduleMessageId == originalMessage.ScheduleMessageId)
                .When(s => s.Handle(originalMessage));

            Assert.That(data.OriginalMessageData, Is.EqualTo(originalMessage));
        }

        [Test]
        public void PauseMessageSetsSchedulePauseFlag_Data()
        {
            var scheduleId = Guid.NewGuid();
            var data = new ScheduledEmailData
                {
                    OriginalMessageData = new OriginalEmailMessageData(new ScheduleEmailForSendingLater
                        {
                            EmailData = new EmailData()
                        }),
                    ScheduleMessageId = scheduleId
                };
            var pauseScheduledMessageIndefinitely = new PauseScheduledMessageIndefinitely(scheduleId);

            Test.Initialize();
            Test.Saga<EmailScheduler>()
                .WithExternalDependencies(a => { a.Data = data; })
                .WhenReceivesMessageFrom("place")
                .ExpectSendLocal<ScheduleStatusChanged>(s =>
                    {
                        return s.Status == MessageStatus.Paused &&
                               s.ScheduleId == pauseScheduledMessageIndefinitely.ScheduleMessageId &&
                               s.RequestTimeUtc == pauseScheduledMessageIndefinitely.MessageRequestTimeUtc;
                    })

                .When(s => s.Handle(pauseScheduledMessageIndefinitely));

            Assert.IsTrue(data.SchedulingPaused);
        }

        [Test]
        public void EmailStatusUpdate_EmailIsOpened_PublishSuccess()
        {
            const EmailStatus emailStatus = EmailStatus.Opened;
            var emailData = new EmailData
                {
                    ToAddress = "toaddress"
                };
            var requestMessage = new ScheduleEmailForSendingLater(DateTime.Now.AddMinutes(5), emailData, new SmsMetaData(), Guid.NewGuid(), "username");
            var data = new ScheduledEmailData
                {
                    ScheduleMessageId = requestMessage.CorrelationId,
                    OriginalMessageData = new OriginalEmailMessageData(requestMessage)
                };
            var emailStatusUpdate = new EmailStatusUpdate { Status = emailStatus };

            Test.Initialize();
            Test.Saga<EmailScheduler>()
                .WithExternalDependencies(a => { a.Data = data; })
                .WhenReceivesMessageFrom("address")
                .ExpectPublish<ScheduledEmailSent>(m =>
                    m.CoordinatorId == requestMessage.CorrelationId
                    && m.EmailStatus == emailStatus
                    && m.ScheduledSmsId == data.ScheduleMessageId
                    && m.ToAddress == emailData.ToAddress
                    && m.Username == requestMessage.Username)
                .ExpectSendLocal<ScheduleStatusChanged>(s => 
                    s.Status == MessageStatus.Sent
                    && s.ScheduleId == data.ScheduleMessageId)
                .When(s => s.Handle(emailStatusUpdate))
                .AssertSagaCompletionIs(true);
        }

        [Test]
        public void EmailStatusUpdate_EmailIsClicked_PublishSuccess()
        {
            const EmailStatus emailStatus = EmailStatus.Clicked;
            var emailData = new EmailData
            {
                ToAddress = "toaddress"
            };
            var requestMessage = new ScheduleEmailForSendingLater(DateTime.Now.AddMinutes(5), emailData, new SmsMetaData(), Guid.NewGuid(), "username");
            var data = new ScheduledEmailData
            {
                ScheduleMessageId = requestMessage.CorrelationId,
                OriginalMessageData = new OriginalEmailMessageData(requestMessage)
            };
            var emailStatusUpdate = new EmailStatusUpdate { Status = emailStatus };

            Test.Initialize();
            Test.Saga<EmailScheduler>()
                .WithExternalDependencies(a => { a.Data = data; })
                .WhenReceivesMessageFrom("address")
                .ExpectPublish<ScheduledEmailSent>(m =>
                    m.CoordinatorId == requestMessage.CorrelationId
                    && m.EmailStatus == emailStatus
                    && m.ScheduledSmsId == data.ScheduleMessageId
                    && m.ToAddress == emailData.ToAddress
                    && m.Username == requestMessage.Username)
                .ExpectSendLocal<ScheduleStatusChanged>(s =>
                    s.Status == MessageStatus.Sent
                    && s.ScheduleId == data.ScheduleMessageId)
                .When(s => s.Handle(emailStatusUpdate))
                .AssertSagaCompletionIs(true);
        }
        [Test]
        public void EmailStatusUpdate_EmailIsDelivered_SetsTimeoutForFurtherInformation_TimeoutExpires_PublishSuccess()
        {
            const EmailStatus emailStatus = EmailStatus.Delivered;
            var emailData = new EmailData
            {
                ToAddress = "toaddress"
            };
            var requestMessage = new ScheduleEmailForSendingLater(DateTime.Now.AddMinutes(5), emailData, new SmsMetaData(), Guid.NewGuid(), "username");
            var data = new ScheduledEmailData
            {
                ScheduleMessageId = requestMessage.CorrelationId,
                OriginalMessageData = new OriginalEmailMessageData(requestMessage)
            };
            var emailStatusUpdate = new EmailStatusUpdate { Status = emailStatus };

            Test.Initialize();
            Test.Saga<EmailScheduler>()
                .WithExternalDependencies(a => { a.Data = data; })
                .WhenReceivesMessageFrom("address")
                    .ExpectTimeoutToBeSetIn<ScheduleEmailDeliveredTimeout>((message, timespan) => timespan.Ticks == new TimeSpan(1, 0, 0, 0).Ticks)
                .When(s => s.Handle(emailStatusUpdate))
                    .ExpectPublish<ScheduledEmailSent>(m =>
                        m.CoordinatorId == requestMessage.CorrelationId
                        && m.EmailStatus == emailStatus
                        && m.ScheduledSmsId == data.ScheduleMessageId
                        && m.ToAddress == emailData.ToAddress
                        && m.Username == requestMessage.Username)
                    .ExpectSendLocal<ScheduleStatusChanged>(s =>
                        s.Status == MessageStatus.Sent
                        && s.ScheduleId == data.ScheduleMessageId)
                .WhenSagaTimesOut()
                .AssertSagaCompletionIs(true);
        }

        [Test]
        public void EmailStatusUpdate_EmailIsFailed_ReplyFailed()
        {
            const EmailStatus emailStatus = EmailStatus.Failed;
            var emailData = new EmailData
            {
                ToAddress = "toaddress"
            };
            var requestMessage = new ScheduleEmailForSendingLater(DateTime.Now.AddMinutes(5), emailData, new SmsMetaData(), Guid.NewGuid(), "username");
            var data = new ScheduledEmailData
            {
                ScheduleMessageId = requestMessage.CorrelationId,
                OriginalMessageData = new OriginalEmailMessageData(requestMessage)
            };
            var emailStatusUpdate = new EmailStatusUpdate { Status = emailStatus };

            Test.Initialize();
            Test.Saga<EmailScheduler>()
                .WithExternalDependencies(a => { a.Data = data; })
                .WhenReceivesMessageFrom("address")
                .ExpectPublish<ScheduledEmailFailed>(m =>
                    m.CoordinatorId == requestMessage.CorrelationId
                    && m.EmailStatus == emailStatus
                    && m.ScheduledSmsId == data.ScheduleMessageId
                    && m.ToAddress == emailData.ToAddress
                    && m.Username == requestMessage.Username)
                .ExpectSendLocal<ScheduleStatusChanged>(s =>
                    s.Status == MessageStatus.Failed
                    && s.ScheduleId == data.ScheduleMessageId)
                .When(s => s.Handle(emailStatusUpdate))
                .AssertSagaCompletionIs(true);
        }

        [Test]
        public void EmailStatusUpdate_EmailIsRejected_ReplyFailed()
        {
            const EmailStatus emailStatus = EmailStatus.Rejected;
            var emailData = new EmailData
            {
                ToAddress = "toaddress"
            };
            var requestMessage = new ScheduleEmailForSendingLater(DateTime.Now.AddMinutes(5), emailData, new SmsMetaData(), Guid.NewGuid(), "username");
            var data = new ScheduledEmailData
            {
                ScheduleMessageId = requestMessage.CorrelationId,
                OriginalMessageData = new OriginalEmailMessageData(requestMessage)
            };
            var emailStatusUpdate = new EmailStatusUpdate { Status = emailStatus };

            Test.Initialize();
            Test.Saga<EmailScheduler>()
                .WithExternalDependencies(a => { a.Data = data; })
                .WhenReceivesMessageFrom("address")
                .ExpectPublish<ScheduledEmailFailed>(m =>
                    m.CoordinatorId == requestMessage.CorrelationId
                    && m.EmailStatus == emailStatus
                    && m.ScheduledSmsId == data.ScheduleMessageId
                    && m.ToAddress == emailData.ToAddress
                    && m.Username == requestMessage.Username)
                .ExpectSendLocal<ScheduleStatusChanged>(s =>
                    s.Status == MessageStatus.Failed
                    && s.ScheduleId == data.ScheduleMessageId)
                .When(s => s.Handle(emailStatusUpdate))
                .AssertSagaCompletionIs(true);
        }
        [Test]
        public void EmailStatusUpdate_EmailIsUnsubscribed_ReplyFailed()
        {
            const EmailStatus emailStatus = EmailStatus.Unsubscribed;
            var emailData = new EmailData
            {
                ToAddress = "toaddress"
            };
            var requestMessage = new ScheduleEmailForSendingLater(DateTime.Now.AddMinutes(5), emailData, new SmsMetaData(), Guid.NewGuid(), "username");
            var data = new ScheduledEmailData
            {
                ScheduleMessageId = requestMessage.CorrelationId,
                OriginalMessageData = new OriginalEmailMessageData(requestMessage)
            };
            var emailStatusUpdate = new EmailStatusUpdate { Status = emailStatus };

            Test.Initialize();
            Test.Saga<EmailScheduler>()
                .WithExternalDependencies(a => { a.Data = data; })
                .WhenReceivesMessageFrom("address")
                .ExpectPublish<ScheduledEmailFailed>(m =>
                    m.CoordinatorId == requestMessage.CorrelationId
                    && m.EmailStatus == emailStatus
                    && m.ScheduledSmsId == data.ScheduleMessageId
                    && m.ToAddress == emailData.ToAddress
                    && m.Username == requestMessage.Username)
                .ExpectSendLocal<ScheduleStatusChanged>(s =>
                    s.Status == MessageStatus.Failed
                    && s.ScheduleId == data.ScheduleMessageId)
                .When(s => s.Handle(emailStatusUpdate))
                .AssertSagaCompletionIs(true);
        }

        [Test]
        public void EmailStatusUpdate_EmailIsComplained_ReplyFailed()
        {
            const EmailStatus emailStatus = EmailStatus.Complained;
            var emailData = new EmailData
            {
                ToAddress = "toaddress"
            };
            var requestMessage = new ScheduleEmailForSendingLater(DateTime.Now.AddMinutes(5), emailData, new SmsMetaData(), Guid.NewGuid(), "username");
            var data = new ScheduledEmailData
            {
                ScheduleMessageId = requestMessage.CorrelationId,
                OriginalMessageData = new OriginalEmailMessageData(requestMessage)
            };
            var emailStatusUpdate = new EmailStatusUpdate { Status = emailStatus };

            Test.Initialize();
            Test.Saga<EmailScheduler>()
                .WithExternalDependencies(a => { a.Data = data; })
                .WhenReceivesMessageFrom("address")
                .ExpectPublish<ScheduledEmailFailed>(m =>
                    m.CoordinatorId == requestMessage.CorrelationId
                    && m.EmailStatus == emailStatus
                    && m.ScheduledSmsId == data.ScheduleMessageId
                    && m.ToAddress == emailData.ToAddress
                    && m.Username == requestMessage.Username)
                .ExpectSendLocal<ScheduleStatusChanged>(s =>
                    s.Status == MessageStatus.Failed
                    && s.ScheduleId == data.ScheduleMessageId)
                .When(s => s.Handle(emailStatusUpdate))
                .AssertSagaCompletionIs(true);
        }
    }
}