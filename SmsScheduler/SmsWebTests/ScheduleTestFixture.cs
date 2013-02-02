using System;
using System.Web.Mvc;
using ConfigurationModels;
using NServiceBus;
using NUnit.Framework;
using Raven.Client;
using Rhino.Mocks;
using SmsMessages.Scheduling.Commands;
using SmsWeb;
using SmsWeb.Controllers;
using SmsWeb.Models;

namespace SmsWebTests
{
    [TestFixture]
    public class ScheduleTestFixture
    {
        [Test]
        public void ScheduleInvalidReturnsToCreatePage()
        {
            var controller = new ScheduleController { ControllerContext = new ControllerContext() };
            var sendNowModel = new ScheduleModel { Number = "number", ScheduledTime = DateTime.Now.AddHours(1) };
            var result = (ViewResult)controller.Create(sendNowModel);

            Assert.That(result.ViewName, Is.EqualTo("Create"));
        }

        [Test]
        public void ScheduleInvalidDateNotInFutureReturnsToCreatePage()
        {
            var controller = new ScheduleController { ControllerContext = new ControllerContext() };
            var sendNowModel = new ScheduleModel { Number = "number" , ScheduledTime = DateTime.Now.AddHours(-3), MessageBody = "abc" };
            var result = (ViewResult)controller.Create(sendNowModel);

            Assert.That(result.ViewName, Is.EqualTo("Create"));
        }

        [Test]
        public void ScheduleValidCountryCodeReplacementNotSetSendsMessageReturnsToDetails()
        {
            var bus = MockRepository.GenerateMock<IBus>();
            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            var docStore = MockRepository.GenerateMock<IDocumentStore>();
            var docSession = MockRepository.GenerateMock<IDocumentSession>();

            ravenDocStore.Expect(r => r.GetStore()).Return(docStore);
            docStore.Expect(d => d.OpenSession("Configuration")).Return(docSession);
            docSession.Expect(d => d.Load<CountryCodeReplacement>("CountryCodeConfig")).Return(new CountryCodeReplacement());

            var controller = new ScheduleController { ControllerContext = new ControllerContext(), Bus = bus, RavenDocStore = ravenDocStore };
            var scheduledTime = DateTime.Now.AddHours(1);
            var sendNowModel = new ScheduleModel { Number = "number", MessageBody = "m", ScheduledTime = scheduledTime };

            var scheduledMessage = new ScheduleSmsForSendingLater();
            bus.Expect(b => b.Send(Arg<ScheduleSmsForSendingLater>.Is.NotNull))
                .WhenCalled(a => scheduledMessage = ((ScheduleSmsForSendingLater)((object[])a.Arguments[0])[0]));

            var result = (RedirectToRouteResult)controller.Create(sendNowModel);
            
            Assert.That(result.RouteValues["action"], Is.EqualTo("Details"));
            Assert.That(scheduledMessage.SendMessageAtUtc.ToString(), Is.EqualTo(scheduledTime.ToUniversalTime().ToString()));
            Assert.That(scheduledMessage.SmsData.Mobile, Is.EqualTo(sendNowModel.Number));
            Assert.That(scheduledMessage.SmsData.Message, Is.EqualTo(sendNowModel.MessageBody));
        }

        [Test]
        public void ScheduleValidCountryCodeReplacementSendsMessageReturnsToDetails()
        {
            var bus = MockRepository.GenerateMock<IBus>();
            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            var docStore = MockRepository.GenerateMock<IDocumentStore>();
            var docSession = MockRepository.GenerateMock<IDocumentSession>();

            ravenDocStore.Expect(r => r.GetStore()).Return(docStore);
            docStore.Expect(d => d.OpenSession("Configuration")).Return(docSession);
            docSession.Expect(d => d.Load<CountryCodeReplacement>("CountryCodeConfig")).Return(new CountryCodeReplacement { CountryCode = "+61", LeadingNumberToReplace = "x" } );

            var controller = new ScheduleController { ControllerContext = new ControllerContext(), Bus = bus, RavenDocStore = ravenDocStore };
            var scheduledTime = DateTime.Now.AddHours(1);
            var sendNowModel = new ScheduleModel { Number = "xnumber", MessageBody = "m", ScheduledTime = scheduledTime };

            var scheduledMessage = new ScheduleSmsForSendingLater();
            bus.Expect(b => b.Send(Arg<ScheduleSmsForSendingLater>.Is.NotNull))
                .WhenCalled(a => scheduledMessage = ((ScheduleSmsForSendingLater)((object[])a.Arguments[0])[0]));

            var result = (RedirectToRouteResult)controller.Create(sendNowModel);
            
            Assert.That(result.RouteValues["action"], Is.EqualTo("Details"));
            Assert.That(scheduledMessage.SendMessageAtUtc.ToString(), Is.EqualTo(scheduledTime.ToUniversalTime().ToString()));
            Assert.That(scheduledMessage.SmsData.Mobile, Is.EqualTo("+61number"));
            Assert.That(scheduledMessage.SmsData.Message, Is.EqualTo(sendNowModel.MessageBody));
        }

        [Test]
        public void ScheduleValidNullCountryCodeReplacementSendsMessageReturnsToDetails()
        {
            var bus = MockRepository.GenerateMock<IBus>();
            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            var docStore = MockRepository.GenerateMock<IDocumentStore>();
            var docSession = MockRepository.GenerateMock<IDocumentSession>();

            ravenDocStore.Expect(r => r.GetStore()).Return(docStore);
            docStore.Expect(d => d.OpenSession("Configuration")).Return(docSession);
            docSession.Expect(d => d.Load<CountryCodeReplacement>("CountryCodeConfig")).Return(null);

            var controller = new ScheduleController { ControllerContext = new ControllerContext(), Bus = bus, RavenDocStore = ravenDocStore };
            var scheduledTime = DateTime.Now.AddHours(1);
            var sendNowModel = new ScheduleModel { Number = "xnumber", MessageBody = "m", ScheduledTime = scheduledTime };

            var scheduledMessage = new ScheduleSmsForSendingLater();
            bus.Expect(b => b.Send(Arg<ScheduleSmsForSendingLater>.Is.NotNull))
                .WhenCalled(a => scheduledMessage = ((ScheduleSmsForSendingLater)((object[])a.Arguments[0])[0]));

            var result = (RedirectToRouteResult)controller.Create(sendNowModel);
            
            Assert.That(result.RouteValues["action"], Is.EqualTo("Details"));
            Assert.That(scheduledMessage.SendMessageAtUtc.ToString(), Is.EqualTo(scheduledTime.ToUniversalTime().ToString()));
            Assert.That(scheduledMessage.SmsData.Mobile, Is.EqualTo(sendNowModel.Number));
            Assert.That(scheduledMessage.SmsData.Message, Is.EqualTo(sendNowModel.MessageBody));
        }
    }
}