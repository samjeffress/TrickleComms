using System;
using NUnit.Framework;
using Rhino.Mocks;
using SmsActioner;
using SmsMessages.CommonData;
using Twilio;

namespace SmsActionerTests
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
                .Return(new SmsQueued(sid));

            var smsService = new SmsService { TwilioWrapper = twilioWrapper };
            smsService.CheckStatus(sid);
        }

        [Test]
        public void CheckMessageIsSent()
        {
            var twilioWrapper = MockRepository.GenerateMock<ITwilioWrapper>();
            const string sid = "sid";
            twilioWrapper
                .Expect(t => t.CheckMessage(sid))
                .Return(new SmsSent(new SmsConfirmationData("receipt", DateTime.Now, 0.04m)));

            var smsService = new SmsService { TwilioWrapper = twilioWrapper };
            smsService.CheckStatus(sid);
        }

        [Test]
        public void CheckMessageIsSending()
        {
            var twilioWrapper = MockRepository.GenerateMock<ITwilioWrapper>();
            const string sid = "sid";
            twilioWrapper
                .Expect(t => t.CheckMessage(sid))
                .Return(new SmsSending(sid));

            var smsService = new SmsService { TwilioWrapper = twilioWrapper };
            smsService.CheckStatus(sid);
        }

        [Test]
        public void CheckMessageIsFailed()
        {
            var twilioWrapper = MockRepository.GenerateMock<ITwilioWrapper>();
            const string sid = "sid";
            twilioWrapper
                .Expect(t => t.CheckMessage(sid))
                .Return(new SmsFailed(sid, "failed", "message", "more", "status"));
            
            var smsService = new SmsService { TwilioWrapper = twilioWrapper };
            smsService.CheckStatus(sid);
        }
    }
}