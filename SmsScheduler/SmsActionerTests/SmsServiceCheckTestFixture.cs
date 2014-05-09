using System;
using System.Collections.Generic;
using NUnit.Framework;
using Rhino.Mocks;
using SmsActioner;
using SmsMessages.CommonData;
using TransmitSms.Models.Recipients;
using TransmitSms.Models.Sms;
using SmsSentResponse = SmsActioner.SmsSentResponse;

namespace SmsActionerTests
{
    [TestFixture]
    public class SmsServiceCheckTestFixture
    {
        [Test]
        public void CheckMessageIsPending()
        {
            var smsTechWrapper = MockRepository.GenerateMock<ISmsTechWrapper>();
            const string sid = "sid";
            smsTechWrapper
                .Expect(t => t.CheckMessage(sid))
                .Return(new SmsSentResponse {  Recipients = new List<RecipientForSms> { new RecipientForSms { DeliveryStatus = "pending" }}});

            var smsService = new SmsService { SmsTechWrapper = smsTechWrapper };
            var smsStatus = smsService.CheckStatus(sid);

            Assert.That(smsStatus, Is.TypeOf(typeof(SmsQueued)));
            Assert.That(smsStatus.Sid, Is.EqualTo(sid));
            smsTechWrapper.VerifyAllExpectations();
        }

        [Test]
        public void CheckMessageIsSent()
        {
            var smsTechWrapper = MockRepository.GenerateMock<ISmsTechWrapper>();
            const string sid = "123";
            var sendAt = DateTime.Now;
            smsTechWrapper
                .Expect(t => t.CheckMessage(sid))
                .Return(new SmsSentResponse
                    {
                        Recipients = new List<RecipientForSms> { new RecipientForSms { DeliveryStatus = "delivered" } },
                        Message = new SmsResponseBase { SendAt = sendAt, MessageId = Convert.ToInt32(sid) }
                    });

            var smsService = new SmsService { SmsTechWrapper = smsTechWrapper };
            var smsStatus = smsService.CheckStatus(sid);

            Assert.That(smsStatus, Is.TypeOf(typeof(SmsSent)));
            Assert.That(smsStatus.Sid, Is.EqualTo(sid));
            var smsSent = smsStatus as SmsSent;
            Assert.That(smsSent.SentAtUtc, Is.EqualTo(sendAt));
            smsTechWrapper.VerifyAllExpectations();
        }

        [Test]
        public void CheckMessageIsFailed_HardBounce()
        {
            var smsTechWrapper = MockRepository.GenerateMock<ISmsTechWrapper>();
            const string sid = "sid";
            smsTechWrapper
                .Expect(t => t.CheckMessage(sid))
                .Return(new SmsSentResponse { Recipients = new List<RecipientForSms> { new RecipientForSms { DeliveryStatus = "hard-bounce" } } });
            
            var smsService = new SmsService { SmsTechWrapper = smsTechWrapper };
            var smsStatus = smsService.CheckStatus(sid);
            
            Assert.That(smsStatus, Is.TypeOf(typeof(SmsFailed)));
            var smsFailed = smsStatus as SmsFailed;
            Assert.That(smsStatus.Sid, Is.EqualTo(sid));
            Assert.That(smsFailed.Code, Is.EqualTo("hard-bounce"));
            Assert.That(smsFailed.Message, Is.EqualTo("The number is invalid or disconnected."));
            smsTechWrapper.VerifyAllExpectations();
        }

        [Test]
        public void CheckMessageIsFailed_SoftBounce()
        {
            var smsTechWrapper = MockRepository.GenerateMock<ISmsTechWrapper>();
            const string sid = "sid";
            smsTechWrapper
                .Expect(t => t.CheckMessage(sid))
                .Return(new SmsSentResponse { Recipients = new List<RecipientForSms> { new RecipientForSms { DeliveryStatus = "soft-bounce" } } });
            
            var smsService = new SmsService { SmsTechWrapper = smsTechWrapper };
            var smsStatus = smsService.CheckStatus(sid);
            
            Assert.That(smsStatus, Is.TypeOf(typeof(SmsFailed)));
            var smsFailed = smsStatus as SmsFailed;
            Assert.That(smsStatus.Sid, Is.EqualTo(sid));
            Assert.That(smsFailed.Code, Is.EqualTo("soft-bounce"));
            Assert.That(smsFailed.Message, Is.EqualTo("The message timed out after 72 hrs, either the recipient was out of range, their phone was off for longer than 72 hrs or the message was unable to be delivered due to a network outage or other connectivity issue."));
            smsTechWrapper.VerifyAllExpectations();
        }
    }
}