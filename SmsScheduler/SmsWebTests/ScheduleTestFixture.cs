using System;
using System.Web.Mvc;
using NServiceBus;
using NUnit.Framework;
using Rhino.Mocks;
using SmsMessages.Scheduling.Commands;
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
            var sendNowModel = new FormCollection { { "Number", "number" }, { "ScheduledTime", DateTime.Now.AddHours(1).ToString() } };
            var result = (ViewResult)controller.Create(sendNowModel);

            Assert.That(result.ViewName, Is.EqualTo("Create"));
        }

        [Test]
        public void ScheduleInvalidDateNotInFutureReturnsToCreatePage()
        {
            var controller = new ScheduleController { ControllerContext = new ControllerContext() };
            var sendNowModel = new FormCollection { { "Number", "number" }, { "ScheduledTime", DateTime.Now.AddHours(-3).ToString() }, { "MessageBody", "abc" } };
            var result = (ViewResult)controller.Create(sendNowModel);

            Assert.That(result.ViewName, Is.EqualTo("Create"));
        }

        [Test]
        public void ScheduleValidSendsMessageReturnsToDetails()
        {
            var bus = MockRepository.GenerateMock<IBus>();
            var controller = new ScheduleController { ControllerContext = new ControllerContext(), Bus = bus};
            var scheduledTime = DateTime.Now.AddHours(1);
            var sendNowModel = new FormCollection { { "Number", "number" }, { "MessageBody", "m" }, { "ScheduledTime", scheduledTime.ToString() } };

            var scheduledMessage = new ScheduleSmsForSendingLater();
            bus.Expect(b => b.Send(Arg<ScheduleSmsForSendingLater>.Is.NotNull))
                .WhenCalled(a => scheduledMessage = ((ScheduleSmsForSendingLater)((object[])a.Arguments[0])[0]));

            var result = (RedirectToRouteResult)controller.Create(sendNowModel);
            
            Assert.That(result.RouteValues["action"], Is.EqualTo("Details"));
            Assert.That(scheduledMessage.SendMessageAtUtc.ToString(), Is.EqualTo(scheduledTime.ToUniversalTime().ToString()));
            Assert.That(scheduledMessage.SmsData.Mobile, Is.EqualTo(sendNowModel["Number"]));
            Assert.That(scheduledMessage.SmsData.Message, Is.EqualTo(sendNowModel["MessageBody"]));
        }
    }
}