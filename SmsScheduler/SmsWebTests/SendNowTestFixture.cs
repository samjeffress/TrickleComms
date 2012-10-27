using System.Web.Mvc;
using NServiceBus;
using NUnit.Framework;
using Rhino.Mocks;
using SmsMessages.MessageSending;
using SmsWeb.Controllers;
using SmsWeb.Models;

namespace SmsWebTests
{
    [TestFixture]
    public class SendNowTestFixture
    {
        [Test]
        public void InvalidMessageReturnsToCreatePageWithValidation()
        {
            var controller = new SendNowController();
            var sendNowModel = new SendNowModel { Number = "number" };
            var result = (ViewResult)controller.Create(sendNowModel);

            Assert.That(result.ViewName, Is.EqualTo("Create"));
        }

        [Test]
        public void InvalidNumberReturnsToCreatePageWithValidation()
        {
            var controller = new SendNowController();
            var sendNowModel = new SendNowModel { MessageBody = "asdflj" };
            var result = (ViewResult)controller.Create(sendNowModel);

            Assert.That(result.ViewName, Is.EqualTo("Create"));
        }

        [Test]
        public void InvalidConfirmationEmailReturnsToCreatePageWithValidation()
        {
            var controller = new SendNowController();
            var sendNowModel = new SendNowModel { MessageBody = "asdflj", Number = "number"};
            var result = (ViewResult)controller.Create(sendNowModel);

            Assert.That(result.ViewName, Is.EqualTo("Create"));
        }

        [Test]
        public void ValidSendsMessageReturnsToDetails()
        {
            var bus = MockRepository.GenerateMock<IBus>();

            var sentMessage = new SendOneMessageNow();
            bus.Expect(b => b.Send(Arg<SendOneMessageNow>.Is.NotNull))
                .WhenCalled(a => sentMessage = ((SendOneMessageNow)((object[])a.Arguments[0])[0]));

            var controller = new SendNowController { Bus = bus };
            var sendNowModel = new SendNowModel { MessageBody = "asdflj", Number = "number", ConfirmationEmail = "sdakflj"};
            var result = (ViewResult)controller.Create(sendNowModel);

            Assert.That(result.ViewName, Is.EqualTo("Details"));
            Assert.That(sentMessage.SmsData.Mobile, Is.EqualTo(sendNowModel.Number));
            Assert.That(sentMessage.SmsData.Message, Is.EqualTo(sendNowModel.MessageBody));
            Assert.That(sentMessage.ConfirmationEmailAddress, Is.EqualTo(sendNowModel.ConfirmationEmail));
            bus.VerifyAllExpectations();
        }
    }
}
