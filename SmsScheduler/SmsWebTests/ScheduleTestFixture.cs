using System;
using System.Web.Mvc;
using NServiceBus;
using NUnit.Framework;
using Rhino.Mocks;
using SmsMessages.Scheduling;
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
            var sendNowModel = new ScheduleModel { Number = "number", ScheduledTime = DateTime.Now.AddHours(1)};
            var result = (ViewResult)controller.Create(sendNowModel);

            Assert.That(result.ViewName, Is.EqualTo("Create"));
        }

        [Test]
        public void ScheduleInvalidDateNotInFutureReturnsToCreatePage()
        {
            var controller = new ScheduleController { ControllerContext = new ControllerContext() };
            var sendNowModel = new ScheduleModel { Number = "number", ScheduledTime = DateTime.Now.AddHours(-3), MessageBody = "abc"};
            var result = (ViewResult)controller.Create(sendNowModel);

            Assert.That(result.ViewName, Is.EqualTo("Create"));
        }

        [Test]
        public void ScheduleValidSendsMessageReturnsToDetails()
        {
            var bus = MockRepository.GenerateMock<IBus>();
            var controller = new ScheduleController { ControllerContext = new ControllerContext(), Bus = bus};
            var sendNowModel = new ScheduleModel { Number = "number", MessageBody = "m", ScheduledTime = DateTime.Now.AddHours(1) };

            var scheduledMessage = new ScheduleSmsForSendingLater();
            bus.Expect(b => b.Send(Arg<ScheduleSmsForSendingLater>.Is.NotNull))
                .WhenCalled(a => scheduledMessage = ((ScheduleSmsForSendingLater)((object[])a.Arguments[0])[0]));

            var result = (RedirectToRouteResult)controller.Create(sendNowModel);
            
            Assert.That(result.RouteValues["action"], Is.EqualTo("Details"));
            Assert.That(scheduledMessage.SendMessageAtUtc, Is.EqualTo(sendNowModel.ScheduledTime.ToUniversalTime()));
            Assert.That(scheduledMessage.SmsData.Mobile, Is.EqualTo(sendNowModel.Number));
            Assert.That(scheduledMessage.SmsData.Message, Is.EqualTo(sendNowModel.MessageBody));
        }
    }
}