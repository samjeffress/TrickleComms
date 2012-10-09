using NUnit.Framework;
using Rhino.Mocks;
using SmsCoordinator;
using SmsMessages.Commands;
using SmsMessages.CommonData;
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

            var smsMessage = new SMSMessage { Status = "sent", Sid = "sidReceipt" };
            twilioWrapper
                .Expect(t => t.SendSmsMessage("defaultFrom", messageToSend.SmsData.Mobile, messageToSend.SmsData.Message))
                .Return(smsMessage);

            var response = smsService.Send(messageToSend);

            Assert.That(response, Is.EqualTo(smsMessage.Sid));
            twilioWrapper.VerifyAllExpectations();
        }
    }

}