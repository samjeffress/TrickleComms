using ConfigurationModels;
using NUnit.Framework;
using Raven.Client;
using Rhino.Mocks;
using SmsActioner;
using SmsMessages.CommonData;
using SmsMessages.MessageSending.Commands;

namespace SmsActionerTests
{
    [TestFixture]
    public class SmsServiceTestFixture
    {
        private IRavenDocStore _ravenDocStore;
        private IDocumentSession _docSession;
        private readonly SmsProviderConfiguration _twilioProvider = new SmsProviderConfiguration {SmsProvider = SmsProvider.Twilio};
        private readonly SmsProviderConfiguration _nexmoProvider = new SmsProviderConfiguration {SmsProvider = SmsProvider.Nexmo};

        [SetUp]
        public void Setup()
        {
            _ravenDocStore = MockRepository.GenerateStub<IRavenDocStore>();
            var mockRavenStore = MockRepository.GenerateStub<IDocumentStore>();
            _docSession = MockRepository.GenerateStub<IDocumentSession>();
            _ravenDocStore.Expect(r => r.GetStore()).Return(mockRavenStore);
            _ravenDocStore.Expect(r => r.ConfigurationDatabaseName()).Return("something");
            mockRavenStore.Expect(m => m.OpenSession(Arg<string>.Is.Anything)).Return(_docSession);
        }

        [Test]
        public void SmsUsesNexmo()
        {
            _docSession.Expect(d => d.Load<SmsProviderConfiguration>("SmsProviderConfiguration")).Return(_nexmoProvider);

            var messageToSend = new SendOneMessageNow { SmsData = new SmsData("mobile", "message") };
            var nexmoWrapper = MockRepository.GenerateMock<INexmoWrapper>();
            var smsService = new SmsService { NexmoWrapper = nexmoWrapper, RavenDocStore = _ravenDocStore };

            nexmoWrapper.Expect(t => t.SendSmsMessage(messageToSend.SmsData.Mobile, messageToSend.SmsData.Message));

            smsService.Send(messageToSend);
            nexmoWrapper.VerifyAllExpectations();
        }

        [Test]
        public void SmsUsesTwilio()
        {
            _docSession.Expect(d => d.Load<SmsProviderConfiguration>("SmsProviderConfiguration")).Return(_twilioProvider);

            var messageToSend = new SendOneMessageNow { SmsData = new SmsData("mobile", "message") };
            var twilioWrapper = MockRepository.GenerateMock<ITwilioWrapper>();
            var smsService = new SmsService { TwilioWrapper = twilioWrapper, RavenDocStore = _ravenDocStore };

            twilioWrapper.Expect(t => t.SendSmsMessage(messageToSend.SmsData.Mobile, messageToSend.SmsData.Message));
            smsService.Send(messageToSend);
            twilioWrapper.VerifyAllExpectations();
        }
    }
}