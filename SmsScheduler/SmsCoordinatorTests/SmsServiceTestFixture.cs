using System;
using NUnit.Framework;
using Rhino.Mocks;
using SmsCoordinator;
using SmsMessages.CommonData;
using SmsMessages.MessageSending;
using Twilio;

namespace SmsCoordinatorTests
{
    [TestFixture]
    public class SmsServiceTestFixture
    {
        [Test]
        public void SmsServiceSuccess()
        {
            var messageToSend = new SendOneMessageNow { SmsData = new SmsData("mobile", "message")};
            var twilioWrapper = MockRepository.GenerateMock<ITwilioWrapper>();
            var smsService = new SmsService { TwilioWrapper = twilioWrapper };

            var smsMessage = new SMSMessage { Status = "sent", Sid = "sidReceipt", DateSent = DateTime.Now, Price = 3 };
            twilioWrapper
                .Expect(t => t.SendSmsMessage("defaultFrom", messageToSend.SmsData.Mobile, messageToSend.SmsData.Message))
                .Return(smsMessage);

            var response = smsService.Send(messageToSend);

            Assert.That(response.Receipt, Is.EqualTo(smsMessage.Sid));
            Assert.That(response.SentAt, Is.EqualTo(smsMessage.DateSent));
            Assert.That(response.Price, Is.EqualTo(smsMessage.Price));
            twilioWrapper.VerifyAllExpectations();
        }

        [Test]
        public void SmsServiceSendingWaitsAndSucceeds()
        {
            var messageToSend = new SendOneMessageNow { SmsData = new SmsData("mobile", "message") };
            var twilioWrapper = MockRepository.GenerateMock<ITwilioWrapper>();
            var smsService = new SmsService { TwilioWrapper = twilioWrapper };

            var smsMessageSending = new SMSMessage { Status = "sending", Sid = "sidReceipt" };
            var smsMessageSent = new SMSMessage { Status = "sent", Sid = "sidReceipt", DateSent = DateTime.Now, Price = 33 };
            twilioWrapper
                .Expect(t => t.SendSmsMessage("defaultFrom", messageToSend.SmsData.Mobile, messageToSend.SmsData.Message))
                .Return(smsMessageSending);

            twilioWrapper
                .Expect(t => t.CheckMessage(smsMessageSending.Sid))
                .Return(smsMessageSent);

            var response = smsService.Send(messageToSend);

            Assert.That(response.Receipt, Is.EqualTo(smsMessageSending.Sid));
            Assert.That(response.SentAt, Is.EqualTo(smsMessageSent.DateSent));
            Assert.That(response.Price, Is.EqualTo(smsMessageSent.Price));
            twilioWrapper.VerifyAllExpectations();
        }

        [Test]
        public void SmsServiceSendingWaitsFiveLoopsThrowsException()
        {
            var messageToSend = new SendOneMessageNow { SmsData = new SmsData("mobile", "message") };
            var twilioWrapper = MockRepository.GenerateMock<ITwilioWrapper>();
            var smsService = new SmsService { TwilioWrapper = twilioWrapper };

            var smsMessageSending = new SMSMessage { Status = "sending", Sid = "sidReceipt" };
            twilioWrapper
                .Expect(t => t.SendSmsMessage("defaultFrom", messageToSend.SmsData.Mobile, messageToSend.SmsData.Message))
                .Return(smsMessageSending);
            twilioWrapper
                .Expect(t => t.CheckMessage(smsMessageSending.Sid))
                .Return(smsMessageSending)
                .Repeat.Times(5);

            Assert.That(() => smsService.Send(messageToSend), Throws.Exception.With.Message.Contains("Waited too long for message to send - retry later"));

            twilioWrapper.VerifyAllExpectations();
        }

        [Test]
        public void SmsServiceSendingFailsThrowsException()
        {
            var messageToSend = new SendOneMessageNow { SmsData = new SmsData("mobile", "message") };
            var twilioWrapper = MockRepository.GenerateMock<ITwilioWrapper>();
            var smsService = new SmsService { TwilioWrapper = twilioWrapper };

            var smsMessageSending = new SMSMessage { Status = "failed", Sid = "sidReceipt" };
            twilioWrapper
                .Expect(t => t.SendSmsMessage("defaultFrom", messageToSend.SmsData.Mobile, messageToSend.SmsData.Message))
                .Return(smsMessageSending);

            Assert.That(() => smsService.Send(messageToSend), Throws.Exception.With.Message.Contains("Message sending failed"));
        }

        [Test]
        public void SmsServiceSendingFailsThrowsExceptionWithRestDetails()
        {
            var messageToSend = new SendOneMessageNow { SmsData = new SmsData("mobile", "message") };
            var twilioWrapper = MockRepository.GenerateMock<ITwilioWrapper>();
            var smsService = new SmsService { TwilioWrapper = twilioWrapper };

            var smsMessageSending = new SMSMessage { Status = "failed", Sid = "sidReceipt", RestException = new RestException {Code = "code", Message = "message", MoreInfo = "moreInfo", Status = "status"}};
            twilioWrapper
                .Expect(t => t.SendSmsMessage("defaultFrom", messageToSend.SmsData.Mobile, messageToSend.SmsData.Message))
                .Return(smsMessageSending);

            Assert.That(() => smsService.Send(messageToSend), Throws.Exception.With.Message.Contains("Rest Exception"));
        }

        [Test]
        public void SmsServiceQueuedMessageNotSureWhatToDo()
        {
            var messageToSend = new SendOneMessageNow { SmsData = new SmsData("mobile", "message") };
            var twilioWrapper = MockRepository.GenerateMock<ITwilioWrapper>();
            var smsService = new SmsService { TwilioWrapper = twilioWrapper };

            var smsMessageSending = new SMSMessage { Status = "queued", Sid = "sidReceipt" };
            twilioWrapper
                .Expect(t => t.SendSmsMessage("defaultFrom", messageToSend.SmsData.Mobile, messageToSend.SmsData.Message))
                .Return(smsMessageSending);

            Assert.That(() => smsService.Send(messageToSend), Throws.Exception.With.Message.Contains("Not sure what to do with a queued message"));
        }
    }

}