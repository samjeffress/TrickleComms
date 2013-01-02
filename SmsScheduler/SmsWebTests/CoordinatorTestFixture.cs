using System;
using System.Web.Mvc;
using NServiceBus;
using NUnit.Framework;
using Rhino.Mocks;
using SmsMessages.Coordinator.Commands;
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
            var model = new FormCollection
            { { "numberList", "04040404040" },
                { "Message", "Message" },
                { "StartTime", DateTime.Now.AddHours(2).ToString() },
                { "TimeSeparatorSeconds", "5000" },
                { "tag", "tag1, tag2" },
                { "Topic", "New Feature!"}
            };

            var bus = MockRepository.GenerateMock<IBus>();
            var mapper = MockRepository.GenerateMock<ICoordinatorModelToMessageMapping>();

            mapper.Expect(m => m.MapToTrickleSpacedByPeriod(Arg<CoordinatedSharedMessageModel>.Is.Anything)).Return(new TrickleSmsWithDefinedTimeBetweenEachMessage());
            var trickleMessage = new TrickleSmsWithDefinedTimeBetweenEachMessage();
            bus.Expect(b => b.Send(Arg<TrickleSmsWithDefinedTimeBetweenEachMessage>.Is.NotNull))
                .WhenCalled(i => trickleMessage = (TrickleSmsWithDefinedTimeBetweenEachMessage) ((object[]) (i.Arguments[0]))[0]);

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
            var model = new FormCollection
            {
                {"numberList", "04040404040" },
                {"Message", "Message"},
                {"StartTime", DateTime.Now.AddHours(2).ToString()},
                {"SendAllBy", DateTime.Now.AddHours(3).ToString()}
            };

            var bus = MockRepository.GenerateMock<IBus>();
            var mapper = MockRepository.GenerateMock<ICoordinatorModelToMessageMapping>();

            mapper.Expect(m => m.MapToTrickleOverPeriod(Arg<CoordinatedSharedMessageModel>.Is.Anything)).Return(new TrickleSmsOverCalculatedIntervalsBetweenSetDates());
            var trickleMessage = new TrickleSmsOverCalculatedIntervalsBetweenSetDates();
            bus.Expect(b => b.Send(Arg<TrickleSmsOverCalculatedIntervalsBetweenSetDates>.Is.NotNull))
                .WhenCalled(i => trickleMessage = (TrickleSmsOverCalculatedIntervalsBetweenSetDates)((object[])(i.Arguments[0]))[0]);

            var controller = new CoordinatorController { ControllerContext = new ControllerContext(), Bus = bus, Mapper = mapper };
            var actionResult = (RedirectToRouteResult)controller.Create(model);

            Assert.That(actionResult.RouteValues["action"], Is.EqualTo("Details"));
            Assert.That(trickleMessage.CoordinatorId, Is.Not.EqualTo(Guid.Empty));

            bus.VerifyAllExpectations();
            mapper.VerifyAllExpectations();
        }

        [Test]
        public void CoordinatorContainsNoNumbersError()
        {
            var bus = MockRepository.GenerateMock<IBus>();
            var controller = new CoordinatorController { ControllerContext = new ControllerContext(), Bus = bus };
            var model = new FormCollection
            {
                {"numberList", "" },
                {"Message", "message" },
                {"StartTime", DateTime.Now.AddHours(2).ToString()},
                {"SendAllBy", DateTime.Now.AddHours(3).ToString()}
            };
            var actionResult = (ViewResult)controller.Create(model);

            Assert.That(actionResult.ViewName, Is.EqualTo("Create"));
        }

        [Test]
        public void CoordinatorContainsNoMessagesError()
        {
            var bus = MockRepository.GenerateMock<IBus>();
            var controller = new CoordinatorController { ControllerContext = new ControllerContext(), Bus = bus };
            var model = new FormCollection
            {
                {"numberList", "04040404040" },
                {"Message", string.Empty },
                {"StartTime", DateTime.Now.AddHours(2).ToString()},
                {"SendAllBy", DateTime.Now.AddHours(3).ToString()}
            };
            var actionResult = (ViewResult)controller.Create(model);

            Assert.That(actionResult.ViewName, Is.EqualTo("Create"));
        }

        [Test]
        public void CoordinatorTimeInPastError()
        {
            var bus = MockRepository.GenerateMock<IBus>();
            var controller = new CoordinatorController { ControllerContext = new ControllerContext(), Bus = bus };
            var model = new FormCollection
            {
                {"numberList", "04040404040" },
                {"Message", "Message"},
                {"StartTime", DateTime.Now.AddHours(-2).ToString()},
                {"SendAllBy", DateTime.Now.AddHours(3).ToString()}
            };
            var actionResult = (ViewResult)controller.Create(model);

            Assert.That(actionResult.ViewName, Is.EqualTo("Create"));
        }

        [Test]
        public void CoordinatorTimeSeparatorNotDefinedError()
        {
            var bus = MockRepository.GenerateMock<IBus>();
            var controller = new CoordinatorController { ControllerContext = new ControllerContext(), Bus = bus };
            var model = new FormCollection
            {
                {"Numbers", "04040404040" },
                {"Message", "Message"},
                {"StartTime", DateTime.Now.AddHours(2).ToString() }
            };
            var actionResult = (ViewResult)controller.Create(model);

            Assert.That(actionResult.ViewName, Is.EqualTo("Create"));
        }
    }
}