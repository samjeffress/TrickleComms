using NUnit.Framework;
using Rhino.Mocks;
using SmsCoordinator;
using SmsMessages.CommonData;
using Twilio;

namespace SmsCoordinatorTests
{
    [TestFixture]
    public class SmsServiceCheckTestFixture
    {
        [Test]
        public void CheckMessageIsQueued()
        {
            var twilioWrapper = MockRepository.GenerateMock<ITwilioWrapper>();
            const string sid = "sid";
            twilioWrapper
                .Expect(t => t.CheckMessage(sid))
                .Return(new SMSMessage { Sid = sid, Status = "queued" });

            var smsService = new SmsService { TwilioWrapper = twilioWrapper };
            var smsStatus = smsService.CheckStatus(sid);

            Assert.That(smsStatus, Is.TypeOf(typeof(SmsQueued)));
            Assert.That(smsStatus.Sid, Is.EqualTo(sid));
            twilioWrapper.VerifyAllExpectations();
        }

        [Test]
        public void CheckMessageIsSent()
        {
            var twilioWrapper = MockRepository.GenerateMock<ITwilioWrapper>();
            const string sid = "sid";
            twilioWrapper
                .Expect(t => t.CheckMessage(sid))
                .Return(new SMSMessage { Sid = sid, Status = "sent" });

            var smsService = new SmsService { TwilioWrapper = twilioWrapper };
            var smsStatus = smsService.CheckStatus(sid);

            Assert.That(smsStatus, Is.TypeOf(typeof(SmsSent)));
            Assert.That(smsStatus.Sid, Is.EqualTo(sid));
            twilioWrapper.VerifyAllExpectations();
        }

        [Test]
        public void CheckMessageIsSending()
        {
            var twilioWrapper = MockRepository.GenerateMock<ITwilioWrapper>();
            const string sid = "sid";
            twilioWrapper
                .Expect(t => t.CheckMessage(sid))
                .Return(new SMSMessage { Sid = sid, Status = "sending" });

            var smsService = new SmsService { TwilioWrapper = twilioWrapper };
            var smsStatus = smsService.CheckStatus(sid);

            Assert.That(smsStatus, Is.TypeOf(typeof(SmsSending)));
            Assert.That(smsStatus.Sid, Is.EqualTo(sid));
            twilioWrapper.VerifyAllExpectations();
        }

        [Test]
        public void CheckMessageIsFailed()
        {
            var twilioWrapper = MockRepository.GenerateMock<ITwilioWrapper>();
            const string sid = "sid";
            twilioWrapper
                .Expect(t => t.CheckMessage(sid))
                .Return(new SMSMessage { Sid = sid, Status = "failed", RestException = new RestException() });
            
            var smsService = new SmsService { TwilioWrapper = twilioWrapper };
            var smsStatus = smsService.CheckStatus(sid);
            
            Assert.That(smsStatus, Is.TypeOf(typeof(SmsFailed)));
            Assert.That(smsStatus.Sid, Is.EqualTo(sid));
            twilioWrapper.VerifyAllExpectations();
        }
    }
}