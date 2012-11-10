using System;
using System.Collections.Generic;
using System.Web.Mvc;
using NServiceBus;
using NUnit.Framework;
using Rhino.Mocks;
using SmsMessages.Coordinator;
using SmsWeb.Controllers;
using SmsWeb.Models;

namespace SmsWebTests
{
    [TestFixture]
    public class CoordinatorTestFixture
    {
        [Test]        
        public void CoordinatorSeparatedByTimeSpanReturnsDetails()
        {
            var bus = MockRepository.GenerateMock<IBus>();

            var trickleByTimePeriod = new TrickleSmsSpacedByTimePeriod();
            bus.Expect(b => b.Send(Arg<TrickleSmsSpacedByTimePeriod>.Is.NotNull))
                .WhenCalled(a => trickleByTimePeriod = ((TrickleSmsSpacedByTimePeriod)((object[])a.Arguments[0])[0]));

            var controller = new CoordinatorController { ControllerContext = new ControllerContext(), Bus = bus };
            var model = new CoordinatedSharedMessageModel
                            {
                                Numbers = new List<string> { "04040404040"},
                                Message = "Message",
                                StartTime = DateTime.Now.AddHours(2),
                                TimeSeparator = new TimeSpan(5000)
                            };
            var actionResult = (RedirectToRouteResult)controller.Create(model);

            Assert.That(actionResult.RouteValues["action"], Is.EqualTo("Details"));
            //Assert.That(trickleByTimePeriod., Is);

            bus.VerifyAllExpectations();
        }

        [Test]
        public void CoordinatorOverTimespanReturnsDetails()
        {
            var bus = MockRepository.GenerateMock<IBus>();

            var trickleByTimePeriod = new TrickleSmsOverTimePeriod();
            bus.Expect(b => b.Send(Arg<TrickleSmsOverTimePeriod>.Is.NotNull))
                .WhenCalled(a => trickleByTimePeriod = ((TrickleSmsOverTimePeriod)((object[])a.Arguments[0])[0]));

            var controller = new CoordinatorController { ControllerContext = new ControllerContext(), Bus = bus };
            var model = new CoordinatedSharedMessageModel
            {
                Numbers = new List<string> { "04040404040" },
                Message = "Message",
                StartTime = DateTime.Now.AddHours(2),
                SendAllBy = DateTime.Now.AddHours(3)
            };
            var actionResult = (RedirectToRouteResult)controller.Create(model);

            Assert.That(actionResult.RouteValues["action"], Is.EqualTo("Details"));

            bus.VerifyAllExpectations();
        }

        [Test]
        public void CoordinatorContainsNoMessagesError()
        {
            var bus = MockRepository.GenerateMock<IBus>();
            var controller = new CoordinatorController { ControllerContext = new ControllerContext(), Bus = bus };
            var model = new CoordinatedSharedMessageModel
            {
                Numbers = new List<string> { "04040404040" },
                Message = string.Empty,
                StartTime = DateTime.Now.AddHours(2),
                SendAllBy = DateTime.Now.AddHours(3)
            };
            var actionResult = (ViewResult)controller.Create(model);

            Assert.That(actionResult.ViewName, Is.EqualTo("Create"));
        }

        [Test]
        public void CoordinatorTimeInPastError()
        {
            var bus = MockRepository.GenerateMock<IBus>();
            var controller = new CoordinatorController { ControllerContext = new ControllerContext(), Bus = bus };
            var model = new CoordinatedSharedMessageModel
            {
                Numbers = new List<string> { "04040404040" },
                Message = "Message",
                StartTime = DateTime.Now.AddHours(-2),
                SendAllBy = DateTime.Now.AddHours(3)
            };
            var actionResult = (ViewResult)controller.Create(model);

            Assert.That(actionResult.ViewName, Is.EqualTo("Create"));
        }

        [Test]
        public void CoordinatorTimeSeparatorNotDefinedError()
        {
            var bus = MockRepository.GenerateMock<IBus>();
            var controller = new CoordinatorController { ControllerContext = new ControllerContext(), Bus = bus };
            var model = new CoordinatedSharedMessageModel
            {
                Numbers = new List<string> { "04040404040" },
                Message = "Message",
                StartTime = DateTime.Now.AddHours(2)
            };
            var actionResult = (ViewResult)controller.Create(model);

            Assert.That(actionResult.ViewName, Is.EqualTo("Create"));
        }
    }
}