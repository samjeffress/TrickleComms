using System.Web.Mvc;
using ConfigurationModels;
using NServiceBus;
using NUnit.Framework;
using Raven.Client;
using Rhino.Mocks;
using SmsMessages.MessageSending.Commands;
using SmsWeb;
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
            var controller = new SendNowController { ControllerContext = new ControllerContext() };
            var sendNowModel = new SendNowModel { Number =  "number" };
            var result = (ViewResult)controller.Create(sendNowModel);

            Assert.That(result.ViewName, Is.EqualTo("Create"));
        }

        [Test]
        public void InvalidNumberReturnsToCreatePageWithValidation()
        {
            var controller = new SendNowController { ControllerContext = new ControllerContext() };
            var sendNowModel = new SendNowModel { MessageBody = "asdflj" };
            var result = (ViewResult)controller.Create(sendNowModel);

            Assert.That(result.ViewName, Is.EqualTo("Create"));
        }

        [Test]
        public void ValidNullCountryConfigSendsMessageReturnsToDetails()
        {
            var bus = MockRepository.GenerateMock<IBus>();
            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            var docStore = MockRepository.GenerateMock<IDocumentStore>();
            var docSession = MockRepository.GenerateMock<IDocumentSession>();

            ravenDocStore.Expect(r => r.GetStore()).Return(docStore);
            docStore.Expect(d => d.OpenSession("Configuration")).Return(docSession);
            docSession.Expect(d => d.Load<CountryCodeReplacement>("CountryCodeConfig")).Return(null);

            var sentMessage = new SendOneMessageNow();
            bus.Expect(b => b.Send(Arg<SendOneMessageNow>.Is.NotNull))
                .WhenCalled(a => sentMessage = ((SendOneMessageNow)((object[])a.Arguments[0])[0]));

            var controller = new SendNowController { ControllerContext = new ControllerContext(), Bus = bus, RavenDocStore = ravenDocStore };
            var sendNowModel = new SendNowModel { MessageBody = "asdflj", Number = "number" , ConfirmationEmail = "sdakflj" };
            var result = (RedirectToRouteResult)controller.Create(sendNowModel);

            Assert.That(result.RouteValues["action"], Is.EqualTo("Details"));
            Assert.That(sentMessage.SmsData.Mobile, Is.EqualTo(sendNowModel.Number));
            Assert.That(sentMessage.SmsData.Message, Is.EqualTo(sendNowModel.MessageBody));
            Assert.That(sentMessage.ConfirmationEmailAddress, Is.EqualTo(sendNowModel.ConfirmationEmail));
            bus.VerifyAllExpectations();
        }

        [Test]
        public void ValidCountryConfigUpdatesNumberSendsMessageReturnsToDetails()
        {
            var bus = MockRepository.GenerateMock<IBus>();
            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            var docStore = MockRepository.GenerateMock<IDocumentStore>();
            var docSession = MockRepository.GenerateMock<IDocumentSession>();

            ravenDocStore.Expect(r => r.GetStore()).Return(docStore);
            docStore.Expect(d => d.OpenSession("Configuration")).Return(docSession);
            docSession.Expect(d => d.Load<CountryCodeReplacement>("CountryCodeConfig")).Return(new CountryCodeReplacement { CountryCode = "+61", LeadingNumberToReplace = "n"});

            var sentMessage = new SendOneMessageNow();
            bus.Expect(b => b.Send(Arg<SendOneMessageNow>.Is.NotNull))
                .WhenCalled(a => sentMessage = ((SendOneMessageNow)((object[])a.Arguments[0])[0]));

            var controller = new SendNowController { ControllerContext = new ControllerContext(), Bus = bus, RavenDocStore = ravenDocStore };
            var sendNowModel = new SendNowModel { MessageBody = "asdflj", Number = "number" , ConfirmationEmail = "sdakflj" };
            var result = (RedirectToRouteResult)controller.Create(sendNowModel);

            Assert.That(result.RouteValues["action"], Is.EqualTo("Details"));
            Assert.That(sentMessage.SmsData.Mobile, Is.EqualTo("+61umber"));
            Assert.That(sentMessage.SmsData.Message, Is.EqualTo(sendNowModel.MessageBody));
            Assert.That(sentMessage.ConfirmationEmailAddress, Is.EqualTo(sendNowModel.ConfirmationEmail));
            bus.VerifyAllExpectations();
        }
    }
}
