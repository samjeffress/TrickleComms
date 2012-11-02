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

            Assert.That(response, Is.TypeOf(typeof(SmsSent)));
            Assert.That(response.Sid, Is.EqualTo(smsMessage.Sid));
            var smsSent = response as SmsSent;
            Assert.That(smsSent.SmsConfirmationData.Receipt, Is.EqualTo(smsMessage.Sid));
            Assert.That(smsSent.SmsConfirmationData.SentAt, Is.EqualTo(smsMessage.DateSent));
            Assert.That(smsSent.SmsConfirmationData.Price, Is.EqualTo(smsMessage.Price));
            twilioWrapper.VerifyAllExpectations();
        }

        //[Test]
        //public void SmsServiceSendingWaitsAndSucceeds()
        //{
        //    var messageToSend = new SendOneMessageNow { SmsData = new SmsData("mobile", "message") };
        //    var twilioWrapper = MockRepository.GenerateMock<ITwilioWrapper>();
        //    var smsService = new SmsService { TwilioWrapper = twilioWrapper };

        //    var smsMessageSending = new SMSMessage { Status = "sending", Sid = "sidReceipt" };
        //    var smsMessageSent = new SMSMessage { Status = "sent", Sid = "sidReceipt", DateSent = DateTime.Now, Price = 33 };
        //    twilioWrapper
        //        .Expect(t => t.SendSmsMessage("defaultFrom", messageToSend.SmsData.Mobile, messageToSend.SmsData.Message))
        //        .Return(smsMessageSending);

        //    twilioWrapper
        //        .Expect(t => t.CheckMessage(smsMessageSending.Sid))
        //        .Return(smsMessageSent);

        //    var response = smsService.Send(messageToSend);

        //    Assert.That(response.Receipt, Is.EqualTo(smsMessageSending.Sid));
        //    Assert.That(response.SentAt, Is.EqualTo(smsMessageSent.DateSent));
        //    Assert.That(response.Price, Is.EqualTo(smsMessageSent.Price));
        //    twilioWrapper.VerifyAllExpectations();
        //}

        //[Test]
        //public void SmsServiceSendingWaitsFiveLoopsThrowsException()
        //{
        //    var messageToSend = new SendOneMessageNow { SmsData = new SmsData("mobile", "message") };
        //    var twilioWrapper = MockRepository.GenerateMock<ITwilioWrapper>();
        //    var smsService = new SmsService { TwilioWrapper = twilioWrapper };

        //    var smsMessageSending = new SMSMessage { Status = "sending", Sid = "sidReceipt" };
        //    twilioWrapper
        //        .Expect(t => t.SendSmsMessage("defaultFrom", messageToSend.SmsData.Mobile, messageToSend.SmsData.Message))
        //        .Return(smsMessageSending);
        //    twilioWrapper
        //        .Expect(t => t.CheckMessage(smsMessageSending.Sid))
        //        .Return(smsMessageSending)
        //        .Repeat.Times(5);

        //    Assert.That(() => smsService.Send(messageToSend), Throws.Exception.With.Message.Contains("Waited too long for message to send - retry later"));

        //    twilioWrapper.VerifyAllExpectations();
        //}

        [Test]
        public void SmsServiceSending()
        {
            var messageToSend = new SendOneMessageNow { SmsData = new SmsData("mobile", "message") };
            var twilioWrapper = MockRepository.GenerateMock<ITwilioWrapper>();
            var smsService = new SmsService { TwilioWrapper = twilioWrapper };

            var smsMessageSending = new SMSMessage { Status = "sending", Sid = "sidReceipt" };
            twilioWrapper
                .Expect(t => t.SendSmsMessage("defaultFrom", messageToSend.SmsData.Mobile, messageToSend.SmsData.Message))
                .Return(smsMessageSending);

            var response = smsService.Send(messageToSend);

            Assert.That(response, Is.TypeOf(typeof (SmsSending)));
            Assert.That(response.Sid, Is.EqualTo(smsMessageSending.Sid));
            twilioWrapper.VerifyAllExpectations();
        }

        [Test]
        public void SmsServiceSendingFails()
        {
            var messageToSend = new SendOneMessageNow { SmsData = new SmsData("mobile", "message") };
            var twilioWrapper = MockRepository.GenerateMock<ITwilioWrapper>();
            var smsService = new SmsService { TwilioWrapper = twilioWrapper };

            var smsMessageSending = new SMSMessage { Status = "failed", Sid = "sidReceipt", RestException = new RestException {Code = "code", Message = "message", MoreInfo = "moreInfo", Status = "status"}};
            twilioWrapper
                .Expect(t => t.SendSmsMessage("defaultFrom", messageToSend.SmsData.Mobile, messageToSend.SmsData.Message))
                .Return(smsMessageSending);

            var response = smsService.Send(messageToSend);

            Assert.That(response, Is.TypeOf(typeof(SmsFailed)));
            Assert.That(response.Sid, Is.EqualTo(smsMessageSending.Sid));
            var smsFailed = response as SmsFailed;
            Assert.That(smsFailed.Status, Is.EqualTo(smsMessageSending.RestException.Status));
            Assert.That(smsFailed.Code, Is.EqualTo(smsMessageSending.RestException.Code));
            Assert.That(smsFailed.Message, Is.EqualTo(smsMessageSending.RestException.Message));
            Assert.That(smsFailed.MoreInfo, Is.EqualTo(smsMessageSending.RestException.MoreInfo));
            twilioWrapper.VerifyAllExpectations();
        }

        [Test]
        public void SmsServiceMessageQueued()
        {
            var messageToSend = new SendOneMessageNow { SmsData = new SmsData("mobile", "message") };
            var twilioWrapper = MockRepository.GenerateMock<ITwilioWrapper>();
            var smsService = new SmsService { TwilioWrapper = twilioWrapper };

            var smsMessageSending = new SMSMessage { Status = "queued", Sid = "sidReceipt" };
            twilioWrapper
                .Expect(t => t.SendSmsMessage("defaultFrom", messageToSend.SmsData.Mobile, messageToSend.SmsData.Message))
                .Return(smsMessageSending);

            var response = smsService.Send(messageToSend);

            Assert.That(response, Is.TypeOf(typeof(SmsQueued)));
            Assert.That(response.Sid, Is.EqualTo(smsMessageSending.Sid));
            twilioWrapper.VerifyAllExpectations();
        }
    }
}