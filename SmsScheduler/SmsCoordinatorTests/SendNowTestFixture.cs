using NServiceBus.Testing;
using NUnit.Framework;
using Rhino.Mocks;
using SmsCoordinator;
using SmsMessages;

namespace SmsCoordinatorTests
{
    [TestFixture]
    public class SendNowTestFixture
    {
        [Test]
        public void SendSinlgeSmsNow()
        {
            var sendOneMessageNow = new SendOneMessageNow();
            const string receipt = "receipt123";

            var smsService = MockRepository.GenerateMock<ISmsService>();
            smsService.Expect(s => s.Send(Arg<SendOneMessageNow>.Is.Anything)).Return(receipt);

            Test.Initialize();

            Test.Handler<SmsActioner>()
                .WithExternalDependencies(m => m.SmsService = smsService)
                .ExpectPublish<MessageSent>(null)
                .OnMessage<SendOneMessageNow>(s => s = sendOneMessageNow);

            smsService.VerifyAllExpectations();
        }
    }
}
