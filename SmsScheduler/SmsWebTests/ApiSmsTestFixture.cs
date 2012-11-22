using System;
using NServiceBus;
using NUnit.Framework;
using Rhino.Mocks;
using SmsMessages.CommonData;
using SmsMessages.MessageSending;
using SmsTracking;
using SmsTrackingTests;
using SmsWeb.API;
using IRavenDocStore = SmsWeb.IRavenDocStore;

namespace SmsWebTests
{
    [TestFixture]
    public class ApiSmsTestFixture : RavenTestBase
    {
        private Guid smsSuccessful = Guid.NewGuid();
        private Guid smsFailed = Guid.NewGuid();

        [Test]
        public void GetSmsNotComplete()
        {
            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            ravenDocStore.Expect(r => r.GetStore()).Return(base.DocumentStore);
            var smsService = new SmsService { RavenDocStore = ravenDocStore };
            var requestId = Guid.NewGuid();
            var request = new Sms { RequestId = requestId };
            var response = smsService.OnGet(request) as SmsResponse;

            Assert.That(response.RequestId, Is.EqualTo(requestId));
            Assert.That(response.ResponseStatus.ErrorCode, Is.EqualTo("NotYetComplete"));
            ravenDocStore.VerifyAllExpectations();
        }

        [Test]
        public void GetSmsSuccessful()
        {
            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            ravenDocStore.Expect(r => r.GetStore()).Return(base.DocumentStore);
            var smsService = new SmsService { RavenDocStore = ravenDocStore };
            var request = new Sms { RequestId = smsSuccessful };
            var response = smsService.OnGet(request) as Sms;

            Assert.That(response.RequestId, Is.EqualTo(smsSuccessful));
            Assert.That(response.Status, Is.EqualTo("Sent"));
            ravenDocStore.VerifyAllExpectations();
        }

        [Test]
        public void GetSmsFailed()
        {
            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            ravenDocStore.Expect(r => r.GetStore()).Return(base.DocumentStore);
            var smsService = new SmsService { RavenDocStore = ravenDocStore };
            var request = new Sms { RequestId = smsFailed };
            var response = smsService.OnGet(request) as Sms;

            Assert.That(response.RequestId, Is.EqualTo(smsFailed));
            Assert.That(response.Status, Is.EqualTo("Failed"));
            ravenDocStore.VerifyAllExpectations();            
        }

        [Test]
        public void PostWithRequestIdSmsSuccess()
        {
            var bus = MockRepository.GenerateMock<IBus>();
            bus.Expect(b => b.Send(Arg<SendOneMessageNow>.Is.Anything));

            var smsService = new SmsService { Bus = bus };
            var request = new Sms { Message = "m", Number = "n", RequestId = Guid.NewGuid() };
            var response = smsService.OnPost(request) as SmsResponse;

            Assert.That(response.RequestId, Is.EqualTo(request.RequestId));
            bus.VerifyAllExpectations();
        }

        [Test]
        public void PostWithoutRequestIdSmsSuccess()
        {
            var bus = MockRepository.GenerateMock<IBus>();
            bus.Expect(b => b.Send(Arg<SendOneMessageNow>.Is.Anything));

            var smsService = new SmsService { Bus = bus };
            var request = new Sms { Message = "m", Number = "n", RequestId = Guid.Empty };
            var response = smsService.OnPost(request) as SmsResponse;

            Assert.That(response.RequestId, Is.Not.EqualTo(Guid.Empty));
            bus.VerifyAllExpectations();
        }

        [Test]
        public void PostInvalidRequest()
        {
            var smsService = new SmsService();
            var request = new Sms { Message = string.Empty, Number = string.Empty, RequestId = Guid.Empty };
            var response = smsService.OnPost(request) as SmsResponse;

            Assert.That(response.ResponseStatus.ErrorCode, Is.EqualTo("InvalidSms"));
        }

        [SetUp]
        public void Setup()
        {
            using (var session = base.DocumentStore.OpenSession())
            {
                var smsSuccessfulTracking = new SmsTrackingData
                {
                    Status = MessageTrackedStatus.Sent, 
                    CorrelationId = smsSuccessful, 
                    SmsData = new SmsData("1", "yo"), 
                    ConfirmationData = new SmsConfirmationData("r", DateTime.Now, 2.0m)
                };
                var smsFailedTracking = new SmsTrackingData
                {
                    Status = MessageTrackedStatus.Failed, 
                    CorrelationId = smsFailed, 
                    SmsData = new SmsData("1", "yo"), 
                    SmsFailureData = new SmsFailed("1", "2", "3", "4", "5")
                };
                session.Store(smsSuccessfulTracking, smsSuccessful.ToString());
                session.Store(smsFailedTracking, smsFailed.ToString());
                session.SaveChanges();
            }
        }
    }
}