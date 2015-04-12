using System;
using NUnit.Framework;
using SmsActioner;
using SmsMessages.CommonData;
using Twilio;

namespace SmsActionerTests
{
    [TestFixture]
    public class TwilioResponseMapping
    {
        [Test]
        public void Queued()
        {
            var twilioResponse = new SMSMessage { Status = "queued", Sid = "sidReceipt" };
            var response = TwilioWrapper.ProcessResponse(twilioResponse);
            Assert.That(response, Is.TypeOf(typeof(SmsQueued)));
            Assert.That(response.Sid, Is.EqualTo(twilioResponse.Sid));
        }

        [Test]
        public void Failed()
        {
            var twilioResponse = new SMSMessage { Status = "failed", Sid = "sidReceipt", RestException = new RestException { Code = "code", Message = "message", MoreInfo = "moreInfo", Status = "status" } };
            var response = TwilioWrapper.ProcessResponse(twilioResponse);

            Assert.That(response, Is.TypeOf(typeof(SmsFailed)));
            Assert.That(response.Sid, Is.EqualTo(twilioResponse.Sid));
            var smsFailed = response as SmsFailed;
            Assert.That(smsFailed.Status, Is.EqualTo(twilioResponse.RestException.Status));
            Assert.That(smsFailed.Code, Is.EqualTo(twilioResponse.RestException.Code));
            Assert.That(smsFailed.Message, Is.EqualTo(twilioResponse.RestException.Message));
            Assert.That(smsFailed.MoreInfo, Is.EqualTo(twilioResponse.RestException.MoreInfo));
        }

        [Test]
        public void FailedRestException()
        {
            var twilioResponse = new SMSMessage { RestException = new RestException { Code = "code", Message = "message", MoreInfo = "moreInfo", Status = "status" } };
            var response = TwilioWrapper.ProcessResponse(twilioResponse);

            Assert.That(response, Is.TypeOf(typeof(SmsFailed)));
            Assert.That(response.Sid, Is.EqualTo(twilioResponse.Sid));
            var smsFailed = response as SmsFailed;
            Assert.That(smsFailed.Status, Is.EqualTo(twilioResponse.RestException.Status));
            Assert.That(smsFailed.Code, Is.EqualTo(twilioResponse.RestException.Code));
            Assert.That(smsFailed.Message, Is.EqualTo(twilioResponse.RestException.Message));
            Assert.That(smsFailed.MoreInfo, Is.EqualTo(twilioResponse.RestException.MoreInfo));
        }

        [Test]
        public void Sending()
        {
            var twilioResponse = new SMSMessage { Status = "sending", Sid = "sidReceipt" };
            var response = TwilioWrapper.ProcessResponse(twilioResponse);

            Assert.That(response, Is.TypeOf(typeof(SmsSending)));
            Assert.That(response.Sid, Is.EqualTo(response.Sid));
        }

        [Test]
        public void Success()
        {
            var twilioResponse = new SMSMessage { Status = "sent", Sid = "sidReceipt", DateSent = DateTime.Now, Price = 3 };
            var response = TwilioWrapper.ProcessResponse(twilioResponse);

            Assert.That(response, Is.TypeOf(typeof(SmsSent)));
            Assert.That(response.Sid, Is.EqualTo(response.Sid));
            var smsSent = response as SmsSent;
            Assert.That(smsSent.SmsConfirmationData.Receipt, Is.EqualTo(twilioResponse.Sid));
            Assert.That(smsSent.SmsConfirmationData.SentAtUtc, Is.EqualTo(twilioResponse.DateSent));
            Assert.That(smsSent.SmsConfirmationData.Price, Is.EqualTo(twilioResponse.Price));
        }
    }
}