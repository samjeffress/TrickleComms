using NUnit.Framework;
using SmsCoordinator;
using SmsMessages;
using SmsMessages.Commands;

namespace SmsCoordinatorTests
{
    [TestFixture]
    public class SmsServiceTestFixture
    {
        [Test]
        public void SmsServiceSuccess()
        {
            var messageToSend = new SendOneMessageNow();

            var smsService = new SmsService();
            var response = smsService.Send(messageToSend);
        }
    }
}