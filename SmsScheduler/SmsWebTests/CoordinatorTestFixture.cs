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
            var model = new CoordinatedSharedMessageModel
            {
                Numbers = new List<string> { "04040404040"},
                Message = "Message",
                StartTime = DateTime.Now.AddHours(2),
                TimeSeparator = new TimeSpan(5000),
                Tags = new List<string> { "tag1", "tag2" },
                Topic = "New Feature!"
            };

            var bus = MockRepository.GenerateMock<IBus>();
            var mapper = MockRepository.GenerateMock<ICoordinatorModelToMessageMapping>();

            mapper.Expect(m => m.MapToTrickleSpacedByPeriod(model)).Return(new TrickleSmsSpacedByTimePeriod());
            var trickleMessage = new TrickleSmsSpacedByTimePeriod();
            bus.Expect(b => b.Send(Arg<TrickleSmsSpacedByTimePeriod>.Is.NotNull))
                .WhenCalled(i => trickleMessage = (TrickleSmsSpacedByTimePeriod) ((object[]) (i.Arguments[0]))[0]);

            var controller = new CoordinatorController { ControllerContext = new ControllerContext(), Bus = bus, Mapper = mapper };
            var actionResult = (RedirectToRouteResult)controller.Create(model);

            Assert.That(actionResult.RouteValues["action"], Is.EqualTo("Details"));
            Assert.That(trickleMessage.CoordinatorId, Is.Not.EqualTo(Guid.Empty));

            bus.VerifyAllExpectations();
            mapper.VerifyAllExpectations();
        }

        [Test]
        public void CoordinatorOverTimespanReturnsDetails()
        {
            var model = new CoordinatedSharedMessageModel
            {
                Numbers = new List<string> { "04040404040" },
                Message = "Message",
                StartTime = DateTime.Now.AddHours(2),
                SendAllBy = DateTime.Now.AddHours(3)
            };

            var bus = MockRepository.GenerateMock<IBus>();
            var mapper = MockRepository.GenerateMock<ICoordinatorModelToMessageMapping>();

            mapper.Expect(m => m.MapToTrickleOverPeriod(model)).Return(new TrickleSmsOverTimePeriod());
            var trickleMessage = new TrickleSmsOverTimePeriod();
            bus.Expect(b => b.Send(Arg<TrickleSmsOverTimePeriod>.Is.NotNull))
                .WhenCalled(i => trickleMessage = (TrickleSmsOverTimePeriod)((object[])(i.Arguments[0]))[0]);

            var controller = new CoordinatorController { ControllerContext = new ControllerContext(), Bus = bus, Mapper = mapper };
            var actionResult = (RedirectToRouteResult)controller.Create(model);

            Assert.That(actionResult.RouteValues["action"], Is.EqualTo("Details"));
            Assert.That(trickleMessage.CoordinatorId, Is.Not.EqualTo(Guid.Empty));

            bus.VerifyAllExpectations();
            mapper.VerifyAllExpectations();
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