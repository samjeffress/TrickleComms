using NUnit.Framework;
using Rhino.Mocks;
using SmsActioner;
using SmsMessages.CommonData;
using SmsMessages.MessageSending.Commands;
using TransmitSms.Models;
using TransmitSms.Models.Sms;

namespace SmsActionerTests
{
    [TestFixture]
    public class SmsServiceTestFixture
    {
        [Test]
        public void SmsServiceSending()
        {
            var messageToSend = new SendOneMessageNow { SmsData = new SmsData("mobile", "message") };
            var smsTechWrapper = MockRepository.GenerateMock<ISmsTechWrapper>();
            var smsService = new SmsService { SmsTechWrapper = smsTechWrapper };

            var smsMessageSending = new SendSmsResponse {Cost = (float) 0.06, MessageId = 123456, Error = new Error { Code = "SUCCESS"} };
            smsTechWrapper
                .Expect(t => t.SendSmsMessage(messageToSend.SmsData.Mobile, messageToSend.SmsData.Message))
                .Return(smsMessageSending);

            var response = smsService.Send(messageToSend);

            Assert.That(response, Is.TypeOf(typeof (SmsSending)));
            Assert.That(response.Sid, Is.EqualTo(smsMessageSending.MessageId.ToString()));
            var smsSending = response as SmsSending;
            Assert.That(smsSending.Price, Is.EqualTo(smsMessageSending.Cost));
            smsTechWrapper.VerifyAllExpectations();
        }

        [Test]
        public void SmsServiceInvalidNumber()
        {
            var messageToSend = new SendOneMessageNow { SmsData = new SmsData("mobile", "message") };
            var smsTechWrapper = MockRepository.GenerateMock<ISmsTechWrapper>();
            var smsService = new SmsService { SmsTechWrapper = smsTechWrapper };

            var smsMessageSending = new SendSmsResponse { Cost = (float)0.06, MessageId = 123456, Error = new Error { Code = "RECIPIENTS_ERROR", Description = "can't send a message to those dudes!"} };
            smsTechWrapper
                .Expect(t => t.SendSmsMessage(messageToSend.SmsData.Mobile, messageToSend.SmsData.Message))
                .Return(smsMessageSending);

            var response = smsService.Send(messageToSend);

            Assert.That(response, Is.TypeOf(typeof(SmsFailed)));
            Assert.That(response.Sid, Is.EqualTo(smsMessageSending.MessageId.ToString()));
            var smsFailed = response as SmsFailed;
            Assert.That(smsFailed.Code, Is.EqualTo(smsMessageSending.Error.Code));
            Assert.That(smsFailed.Message, Is.EqualTo(smsMessageSending.Error.Description));
            smsTechWrapper.VerifyAllExpectations();
        }

        [Test]
        public void SmsServiceOutOfMoney()
        {
            var messageToSend = new SendOneMessageNow { SmsData = new SmsData("mobile", "message") };
            var smsTechWrapper = MockRepository.GenerateMock<ISmsTechWrapper>();
            var smsService = new SmsService { SmsTechWrapper = smsTechWrapper };

            var smsMessageSending = new SendSmsResponse { Cost = (float)0.06, MessageId = 123456, Error = new Error { Code = "LEDGER_ERROR", Description = "can't send a message to those dudes!" } };
            const string accountIsCurrentlyOutOfMoney = "Could not send message - account is currently out of money";
            smsTechWrapper
                .Expect(t => t.SendSmsMessage(messageToSend.SmsData.Mobile, messageToSend.SmsData.Message))
                .Return(smsMessageSending);

            Assert.That(() => smsService.Send(messageToSend), Throws.Exception.TypeOf<AccountOutOfMoneyException>().With.Message.EqualTo(accountIsCurrentlyOutOfMoney));
        }
    }

}