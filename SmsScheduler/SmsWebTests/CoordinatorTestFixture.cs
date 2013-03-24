using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using ConfigurationModels;
using NServiceBus;
using NUnit.Framework;
using Raven.Client;
using Raven.Client.Embedded;
using Rhino.Mocks;
using SmsMessages.CommonData;
using SmsMessages.Coordinator.Commands;
using SmsTrackingModels;
using SmsWeb;
using SmsWeb.Controllers;
using SmsWeb.Models;

namespace SmsWebTests
{
    [TestFixture]
    public class CoordinatorTestFixture
    {
        private IDocumentSession SmsTrackingSession;
        private Guid Top1CoordinatorId;

        [SetUp]
        public void Setup()
        {
            var _store = new EmbeddableDocumentStore { RunInMemory = true };
            _store.Initialize();
            Top1CoordinatorId = Guid.NewGuid();
            var mostRecentCoordinators = new List<CoordinatorTrackingData>
                {
                    new CoordinatorTrackingData
                        {
                            CoordinatorId = Top1CoordinatorId, 
                            MetaData = new SmsMetaData { Topic = "barry" }, 
                            CreationDateUtc = DateTime.Now.AddDays(-3),
                            MessageStatuses = new List<MessageSendingStatus> { new MessageSendingStatus { Status = MessageStatusTracking.CompletedSuccess }}
                        },
                };

            SmsTrackingSession = _store.OpenSession();
            foreach (var coordinatorTrackingData in mostRecentCoordinators)
            {
                SmsTrackingSession.Store(coordinatorTrackingData, coordinatorTrackingData.CoordinatorId.ToString());
            }
            SmsTrackingSession.SaveChanges();
        }

        [Test]        
        public void CoordinatorSeparatedByTimeSpanReturnsDetails()
        {
            var model = new CoordinatedSharedMessageModel
            {
                Numbers= "04040404040",
                Message = "Message",
                StartTime = DateTime.Now.AddHours(2),
                TimeSeparatorSeconds = 5000,
                Tags = "tag1, tag2",
                Topic = "New Feature!"
            };

            var bus = MockRepository.GenerateMock<IBus>();
            var mapper = MockRepository.GenerateMock<ICoordinatorModelToMessageMapping>();
            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            var docStore = MockRepository.GenerateMock<IDocumentStore>();
            var docSession = MockRepository.GenerateMock<IDocumentSession>();

            ravenDocStore.Expect(r => r.GetStore()).Return(docStore);
            docStore.Expect(d => d.OpenSession("Configuration")).Return(docSession);
            docSession.Expect(d => d.Load<CountryCodeReplacement>("CountryCodeConfig")).Return(new CountryCodeReplacement());

            mapper
                .Expect(m => m.MapToTrickleSpacedByPeriod(Arg<CoordinatedSharedMessageModel>.Is.Anything, Arg<CountryCodeReplacement>.Is.Anything, Arg<List<string>>.Is.Anything))
                .Return(new TrickleSmsWithDefinedTimeBetweenEachMessage());
            var trickleMessage = new TrickleSmsWithDefinedTimeBetweenEachMessage();
            bus.Expect(b => b.Send(Arg<TrickleSmsWithDefinedTimeBetweenEachMessage>.Is.NotNull))
                .WhenCalled(i => trickleMessage = (TrickleSmsWithDefinedTimeBetweenEachMessage) ((object[]) (i.Arguments[0]))[0]);

            var controller = new CoordinatorController { ControllerContext = new ControllerContext(), Bus = bus, Mapper = mapper, RavenDocStore = ravenDocStore };
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
                Numbers = "04040404040",
                Message = "Message",
                StartTime = DateTime.Now.AddHours(2),
                SendAllBy = DateTime.Now.AddHours(3),
                Topic = "frank"
            };

            var bus = MockRepository.GenerateMock<IBus>();
            var mapper = MockRepository.GenerateMock<ICoordinatorModelToMessageMapping>();
            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            var docStore = MockRepository.GenerateMock<IDocumentStore>();
            var docSession = MockRepository.GenerateMock<IDocumentSession>();

            ravenDocStore.Expect(r => r.GetStore()).Return(docStore);
            docStore.Expect(d => d.OpenSession("Configuration")).Return(docSession);
            docSession.Expect(d => d.Load<CountryCodeReplacement>("CountryCodeConfig")).Return(new CountryCodeReplacement());

            mapper
                .Expect(m => m.MapToTrickleOverPeriod(Arg<CoordinatedSharedMessageModel>.Is.Anything, Arg<CountryCodeReplacement>.Is.Anything, Arg<List<string>>.Is.Equal(new List<string>())))
                    .Return(new TrickleSmsOverCalculatedIntervalsBetweenSetDates());
            var trickleMessage = new TrickleSmsOverCalculatedIntervalsBetweenSetDates();
            bus.Expect(b => b.Send(Arg<TrickleSmsOverCalculatedIntervalsBetweenSetDates>.Is.NotNull))
                .WhenCalled(i => trickleMessage = (TrickleSmsOverCalculatedIntervalsBetweenSetDates)((object[])(i.Arguments[0]))[0]);

            var controller = new CoordinatorController { ControllerContext = new ControllerContext(), Bus = bus, Mapper = mapper, RavenDocStore = ravenDocStore };
            var actionResult = (RedirectToRouteResult)controller.Create(model);

            Assert.That(actionResult.RouteValues["action"], Is.EqualTo("Details"));
            Assert.That(trickleMessage.CoordinatorId, Is.Not.EqualTo(Guid.Empty));

            bus.VerifyAllExpectations();
            mapper.VerifyAllExpectations();
        }

        [Test]
        public void CoordinatorOverTimespanLongMessageIsShortenedReturnsDetails()
        {
            var model = new CoordinatedSharedMessageModel
            {
                Numbers = "04040404040",
                Message = "asfdkjadfskl asflkj;faskjf;aslkjf;lasdkjfaslkfjas;lkfjslkfjas;lkfjsalkfjas;fklasj;flksdjf;lkasjflskdjflkasjflksjlk lskaf jlsk fdaskl dflksjfalk sflkj sfkl jlkjs flkj skjkj sadflkjsaflj",
                StartTime = DateTime.Now.AddHours(2),
                SendAllBy = DateTime.Now.AddHours(3),
                Topic = "frank"
            };

            var bus = MockRepository.GenerateMock<IBus>();
            var mapper = MockRepository.GenerateMock<ICoordinatorModelToMessageMapping>();
            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            var docStore = MockRepository.GenerateMock<IDocumentStore>();
            var docSession = MockRepository.GenerateMock<IDocumentSession>();

            ravenDocStore.Expect(r => r.GetStore()).Return(docStore);
            docStore.Expect(d => d.OpenSession("Configuration")).Return(docSession);
            docSession.Expect(d => d.Load<CountryCodeReplacement>("CountryCodeConfig")).Return(new CountryCodeReplacement());

            var coordinatorMessage = new CoordinatedSharedMessageModel();
            var excludeList = new List<string>();
            mapper
                .Expect(m => m.MapToTrickleOverPeriod(Arg<CoordinatedSharedMessageModel>.Is.Anything, Arg<CountryCodeReplacement>.Is.Anything, Arg<List<string>>.Is.Anything))
                .Return(new TrickleSmsOverCalculatedIntervalsBetweenSetDates())
                .WhenCalled(t => coordinatorMessage = (CoordinatedSharedMessageModel)(t.Arguments[0]))
                .WhenCalled(t => excludeList = (List<string>)(t.Arguments[2]));
            var trickleMessage = new TrickleSmsOverCalculatedIntervalsBetweenSetDates();
            bus.Expect(b => b.Send(Arg<TrickleSmsOverCalculatedIntervalsBetweenSetDates>.Is.NotNull))
                .WhenCalled(i => trickleMessage = (TrickleSmsOverCalculatedIntervalsBetweenSetDates)((object[])(i.Arguments[0]))[0]);

            var controller = new CoordinatorController { ControllerContext = new ControllerContext(), Bus = bus, Mapper = mapper, RavenDocStore = ravenDocStore };
            var actionResult = (RedirectToRouteResult)controller.Create(model);

            Assert.That(actionResult.RouteValues["action"], Is.EqualTo("Details"));
            Assert.That(coordinatorMessage.Message, Is.EqualTo(model.Message.Substring(0, 160)));
            Assert.That(excludeList.Count, Is.EqualTo(0));

            bus.VerifyAllExpectations();
            mapper.VerifyAllExpectations();
        }

        [Test]
        public void CoordinatorExcludePreviousCoordinatorMessagesRemovesMatchingNumbers()
        {
            var CoordinatorToExclude = Guid.NewGuid();
            var model = new CoordinatedSharedMessageModel
            {
                Numbers = "04040404040, 1, 2, 3",
                Message = "asfdkjadfskl asflkj;faskjf;aslkjf;lasdkjfaslkfjas;lkfjslkfjas;lkfjsalkfjas;fklasj;flksdjf;lkasjflskdjflkasjflksjlk lskaf jlsk fdaskl dflksjfalk sflkj sfkl jlkjs flkj skjkj sadflkjsaflj",
                StartTime = DateTime.Now.AddHours(2),
                SendAllBy = DateTime.Now.AddHours(3),
                CoordinatorsToExclude = new List<Guid> { CoordinatorToExclude },
                Topic = "frank"
            };

            var bus = MockRepository.GenerateMock<IBus>();
            var mapper = MockRepository.GenerateMock<ICoordinatorModelToMessageMapping>();
            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            var docStore = MockRepository.GenerateMock<IDocumentStore>();
            var docSession = MockRepository.GenerateMock<IDocumentSession>();
            var trackingSession = MockRepository.GenerateMock<IDocumentSession>();

            ravenDocStore.Expect(r => r.GetStore()).Return(docStore);
            docStore.Expect(d => d.OpenSession("Configuration")).Return(docSession);
            docStore.Expect(d => d.OpenSession("SmsTracking")).Return(trackingSession);
            docSession.Expect(d => d.Load<CountryCodeReplacement>("CountryCodeConfig")).Return(new CountryCodeReplacement());
            var previousCoordinatorToExclude = new CoordinatorTrackingData { MessageStatuses = new List<MessageSendingStatus> { new MessageSendingStatus { Number = "04040404040" }, new MessageSendingStatus { Number = "1" } } };
            trackingSession.Expect(d => d.Load<CoordinatorTrackingData>(CoordinatorToExclude.ToString())).Return(
                previousCoordinatorToExclude);

            var coordinatorMessage = new CoordinatedSharedMessageModel();
            var excludeList = previousCoordinatorToExclude.MessageStatuses.Select(s => s.Number).ToList();
            mapper
                .Expect(m => m.MapToTrickleOverPeriod(Arg<CoordinatedSharedMessageModel>.Is.Anything, Arg<CountryCodeReplacement>.Is.Anything, Arg<List<string>>.Is.Equal(excludeList)))
                .Return(new TrickleSmsOverCalculatedIntervalsBetweenSetDates())
                .WhenCalled(t => coordinatorMessage = (CoordinatedSharedMessageModel)(t.Arguments[0]));
            bus.Expect(b => b.Send(Arg<TrickleSmsOverCalculatedIntervalsBetweenSetDates>.Is.NotNull));

            var controller = new CoordinatorController { ControllerContext = new ControllerContext(), Bus = bus, Mapper = mapper, RavenDocStore = ravenDocStore };
            var actionResult = (RedirectToRouteResult)controller.Create(model);

            Assert.That(actionResult.RouteValues["action"], Is.EqualTo("Details"));
            Assert.That(coordinatorMessage.Message, Is.EqualTo(model.Message.Substring(0, 160)));

            bus.VerifyAllExpectations();
            mapper.VerifyAllExpectations();
            docSession.VerifyAllExpectations();
            trackingSession.VerifyAllExpectations();
        }

        [Test]
        public void CoordinatorExcludeMultiplePreviousCoordinatorMessagesRemovesMatchingNumbers_TrickleBetweenDates()
        {
            var CoordinatorToExclude1 = Guid.NewGuid();
            var CoordinatorToExclude2 = Guid.NewGuid();
            var model = new CoordinatedSharedMessageModel
            {
                Numbers = "04040404040, 1, 2, 3, 7, 12",
                Message = "asfdkjadfskl asflkj;faskjf;aslkjf;lasdkjfaslkfjas;lkfjslkfjas;lkfjsalkfjas;fklasj;flksdjf;lkasjflskdjflkasjflksjlk lskaf jlsk fdaskl dflksjfalk sflkj sfkl jlkjs flkj skjkj sadflkjsaflj",
                StartTime = DateTime.Now.AddHours(2),
                SendAllBy = DateTime.Now.AddHours(3),
                CoordinatorsToExclude = new List<Guid> { CoordinatorToExclude1, CoordinatorToExclude2 },
                Topic = "frank"
            };

            var bus = MockRepository.GenerateMock<IBus>();
            var mapper = MockRepository.GenerateMock<ICoordinatorModelToMessageMapping>();
            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            var docStore = MockRepository.GenerateMock<IDocumentStore>();
            var configSession = MockRepository.GenerateMock<IDocumentSession>();
            var trackingSession = MockRepository.GenerateMock<IDocumentSession>();

            ravenDocStore.Expect(r => r.GetStore()).Return(docStore);
            docStore.Expect(d => d.OpenSession("Configuration")).Return(configSession);
            docStore.Expect(d => d.OpenSession("SmsTracking")).Return(trackingSession);
            configSession.Expect(d => d.Load<CountryCodeReplacement>("CountryCodeConfig")).Return(new CountryCodeReplacement());
            var previousCoordinatorToExclude1 = new CoordinatorTrackingData { MessageStatuses = new List<MessageSendingStatus> { new MessageSendingStatus { Number = "04040404040" }, new MessageSendingStatus { Number = "1" } } };
            trackingSession.Expect(d => d.Load<CoordinatorTrackingData>(CoordinatorToExclude1.ToString())).Return(
                previousCoordinatorToExclude1);            
            var previousCoordinatorToExclude2 = new CoordinatorTrackingData { MessageStatuses = new List<MessageSendingStatus> { new MessageSendingStatus { Number = "7" } } };
            trackingSession.Expect(d => d.Load<CoordinatorTrackingData>(CoordinatorToExclude2.ToString())).Return(
                previousCoordinatorToExclude2);

            var coordinatorMessage = new CoordinatedSharedMessageModel();
            var excludeList1 = previousCoordinatorToExclude1.MessageStatuses.Select(s => s.Number).ToList();
            var excludeList2 = previousCoordinatorToExclude2.MessageStatuses.Select(s => s.Number).ToList();

            List<string> excludeList = null;
            mapper
                .Expect(m => m.MapToTrickleOverPeriod(Arg<CoordinatedSharedMessageModel>.Is.Anything, Arg<CountryCodeReplacement>.Is.Anything, Arg<List<string>>.Is.Anything))
                .Return(new TrickleSmsOverCalculatedIntervalsBetweenSetDates())
                .WhenCalled(t => coordinatorMessage = (CoordinatedSharedMessageModel)(t.Arguments[0]))
                .WhenCalled(t => excludeList = (List<string>)(t.Arguments[2]));
            bus.Expect(b => b.Send(Arg<TrickleSmsOverCalculatedIntervalsBetweenSetDates>.Is.NotNull));

            var controller = new CoordinatorController { ControllerContext = new ControllerContext(), Bus = bus, Mapper = mapper, RavenDocStore = ravenDocStore };
            var actionResult = (RedirectToRouteResult)controller.Create(model);

            Assert.That(actionResult.RouteValues["action"], Is.EqualTo("Details"));
            Assert.That(coordinatorMessage.Message, Is.EqualTo(model.Message.Substring(0, 160)));
            Assert.That(excludeList.ToList(), Is.EqualTo(excludeList1.Union(excludeList2).Distinct().ToList()));

            bus.VerifyAllExpectations();
            mapper.VerifyAllExpectations();
            configSession.VerifyAllExpectations();
            trackingSession.VerifyAllExpectations();
        }

        [Test]
        public void CoordinatorExcludeMultiplePreviousCoordinatorMessagesRemovesMatchingNumbers_TrickleMessageSpaceDefined()
        {
            var CoordinatorToExclude1 = Guid.NewGuid();
            var CoordinatorToExclude2 = Guid.NewGuid();
            var model = new CoordinatedSharedMessageModel
            {
                Numbers = "04040404040, 1, 2, 3, 7, 12",
                Message = "asfdkjadfskl asflkj;faskjf;aslkjf;lasdkjfaslkfjas;lkfjslkfjas;lkfjsalkfjas;fklasj;flksdjf;lkasjflskdjflkasjflksjlk lskaf jlsk fdaskl dflksjfalk sflkj sfkl jlkjs flkj skjkj sadflkjsaflj",
                StartTime = DateTime.Now.AddHours(2),
                TimeSeparatorSeconds = 3,
                CoordinatorsToExclude = new List<Guid> { CoordinatorToExclude1, CoordinatorToExclude2 },
                Topic = "frank"
            };

            var bus = MockRepository.GenerateMock<IBus>();
            var mapper = MockRepository.GenerateMock<ICoordinatorModelToMessageMapping>();
            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            var docStore = MockRepository.GenerateMock<IDocumentStore>();
            var docSession = MockRepository.GenerateMock<IDocumentSession>();
            var trackingSession = MockRepository.GenerateMock<IDocumentSession>();

            ravenDocStore.Expect(r => r.GetStore()).Return(docStore);
            docStore.Expect(d => d.OpenSession("Configuration")).Return(docSession);
            docStore.Expect(d => d.OpenSession("SmsTracking")).Return(trackingSession);
            docSession.Expect(d => d.Load<CountryCodeReplacement>("CountryCodeConfig")).Return(new CountryCodeReplacement());
            var previousCoordinatorToExclude1 = new CoordinatorTrackingData { MessageStatuses = new List<MessageSendingStatus> { new MessageSendingStatus { Number = "04040404040" }, new MessageSendingStatus { Number = "1" } } };
            trackingSession.Expect(d => d.Load<CoordinatorTrackingData>(CoordinatorToExclude1.ToString())).Return(previousCoordinatorToExclude1);            
            var previousCoordinatorToExclude2 = new CoordinatorTrackingData { MessageStatuses = new List<MessageSendingStatus> { new MessageSendingStatus { Number = "7" } } };
            trackingSession.Expect(d => d.Load<CoordinatorTrackingData>(CoordinatorToExclude2.ToString())).Return(previousCoordinatorToExclude2);

            var coordinatorMessage = new CoordinatedSharedMessageModel();
            var excludeList1 = previousCoordinatorToExclude1.MessageStatuses.Select(s => s.Number).ToList();
            var excludeList2 = previousCoordinatorToExclude2.MessageStatuses.Select(s => s.Number).ToList();

            List<string> excludeList = null;
            mapper
                .Expect(m => m.MapToTrickleSpacedByPeriod(Arg<CoordinatedSharedMessageModel>.Is.Anything, Arg<CountryCodeReplacement>.Is.Anything, Arg<List<string>>.Is.Anything))
                .Return(new TrickleSmsWithDefinedTimeBetweenEachMessage())
                .WhenCalled(t => coordinatorMessage = (CoordinatedSharedMessageModel)(t.Arguments[0]))
                .WhenCalled(t => excludeList = (List<string>)(t.Arguments[2]));
            bus.Expect(b => b.Send(Arg<TrickleSmsOverCalculatedIntervalsBetweenSetDates>.Is.NotNull));

            var controller = new CoordinatorController { ControllerContext = new ControllerContext(), Bus = bus, Mapper = mapper, RavenDocStore = ravenDocStore };
            var actionResult = (RedirectToRouteResult)controller.Create(model);

            Assert.That(actionResult.RouteValues["action"], Is.EqualTo("Details"));
            Assert.That(coordinatorMessage.Message, Is.EqualTo(model.Message.Substring(0, 160)));
            Assert.That(excludeList.ToList(), Is.EqualTo(excludeList1.Union(excludeList2).Distinct().ToList()));

            bus.VerifyAllExpectations();
            mapper.VerifyAllExpectations();
            docSession.VerifyAllExpectations();
            trackingSession.VerifyAllExpectations();
        }

        [Test]
        public void CoordinatorContainsNoNumbersError()
        {
            var bus = MockRepository.GenerateMock<IBus>();
            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            var docStore = MockRepository.GenerateMock<IDocumentStore>();

            ravenDocStore.Expect(r => r.GetStore()).Return(docStore);
            docStore.Expect(d => d.OpenSession("SmsTracking")).Return(SmsTrackingSession);

            var controller = new CoordinatorController { ControllerContext = new ControllerContext(), Bus = bus, RavenDocStore = ravenDocStore };
            var model = new CoordinatedSharedMessageModel
            {
                Numbers = "",
                Message = "message",
                StartTime = DateTime.Now.AddHours(2),
                SendAllBy = DateTime.Now.AddHours(3)
            };
            var actionResult = (ViewResult)controller.Create(model);

            Assert.That(actionResult.ViewName, Is.EqualTo("Create"));
        }

        [Test]
        public void CoordinatorContainsNoMessagesError()
        {
            var bus = MockRepository.GenerateMock<IBus>();
            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            var docStore = MockRepository.GenerateMock<IDocumentStore>();

            ravenDocStore.Expect(r => r.GetStore()).Return(docStore);
            docStore.Expect(d => d.OpenSession("SmsTracking")).Return(SmsTrackingSession);

            var controller = new CoordinatorController { ControllerContext = new ControllerContext(), Bus = bus, RavenDocStore = ravenDocStore };
            var model = new CoordinatedSharedMessageModel
            {
                Numbers = "04040404040",
                Message = string.Empty,
                StartTime = DateTime.Now.AddHours(2),
                SendAllBy = DateTime.Now.AddHours(3)
            };
            var actionResult = (ViewResult)controller.Create(model);

            Assert.That(actionResult.ViewName, Is.EqualTo("Create"));
        }

        [Test]
        public void ErrorWithSelectedCoordinatorsToExclude()
        {
            var bus = MockRepository.GenerateMock<IBus>();
            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            var docStore = MockRepository.GenerateMock<IDocumentStore>();

            ravenDocStore.Expect(r => r.GetStore()).Return(docStore);
            docStore.Expect(d => d.OpenSession("SmsTracking")).Return(SmsTrackingSession);

            var controller = new CoordinatorController { ControllerContext = new ControllerContext(), Bus = bus, RavenDocStore = ravenDocStore };
            var model = new CoordinatedSharedMessageModel
            {
                Numbers = "04040404040",
                Message = string.Empty,
                StartTime = DateTime.Now.AddHours(2),
                SendAllBy = DateTime.Now.AddHours(3),
                CoordinatorsToExclude = new List<Guid> { Top1CoordinatorId }
            };
            var actionResult = (ViewResult)controller.Create(model);
            var selectListItems = actionResult.ViewData["CoordinatorExcludeList"] as List<SelectListItem>;
            Assert.That(actionResult.ViewName, Is.EqualTo("Create"));
            Assert.True(selectListItems.First(s => s.Value == Top1CoordinatorId.ToString()).Selected);
        }

        [Test]
        public void CoordinatorTimeInPastError()
        {
            var bus = MockRepository.GenerateMock<IBus>();
            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            var docStore = MockRepository.GenerateMock<IDocumentStore>();

            ravenDocStore.Expect(r => r.GetStore()).Return(docStore);
            docStore.Expect(d => d.OpenSession("SmsTracking")).Return(SmsTrackingSession);

            var controller = new CoordinatorController { ControllerContext = new ControllerContext(), Bus = bus, RavenDocStore = ravenDocStore };
            var model = new CoordinatedSharedMessageModel
            {
                Numbers = "04040404040",
                Message = "Message",
                StartTime = DateTime.Now.AddHours(-2),
                SendAllBy = DateTime.Now.AddHours(3),
                CoordinatorsToExclude = new List<Guid>()
            };
            var actionResult = (ViewResult)controller.Create(model);

            Assert.That(actionResult.ViewName, Is.EqualTo("Create"));
        }

        [Test]
        public void CoordinatorNoTopicError()
        {
            var bus = MockRepository.GenerateMock<IBus>();
            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            var docStore = MockRepository.GenerateMock<IDocumentStore>();

            ravenDocStore.Expect(r => r.GetStore()).Return(docStore);
            docStore.Expect(d => d.OpenSession("SmsTracking")).Return(SmsTrackingSession);

            var controller = new CoordinatorController { ControllerContext = new ControllerContext(), Bus = bus, RavenDocStore = ravenDocStore };
            var model = new CoordinatedSharedMessageModel
            {
                Numbers = "04040404040",
                Message = "Message",
                StartTime = DateTime.Now.AddHours(2),
                SendAllBy = DateTime.Now.AddHours(3),
                CoordinatorsToExclude = new List<Guid>(),
                Topic = string.Empty
            };
            var actionResult = (ViewResult)controller.Create(model);

            Assert.That(actionResult.ViewName, Is.EqualTo("Create"));
        }

        [Test]
        public void CoordinatorTimeSeparatorNotDefinedError()
        {
            var bus = MockRepository.GenerateMock<IBus>();
            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            var docStore = MockRepository.GenerateMock<IDocumentStore>();

            ravenDocStore.Expect(r => r.GetStore()).Return(docStore);
            docStore.Expect(d => d.OpenSession("SmsTracking")).Return(SmsTrackingSession);

            var controller = new CoordinatorController { ControllerContext = new ControllerContext(), Bus = bus, RavenDocStore = ravenDocStore };
            var model = new CoordinatedSharedMessageModel
            {
                Numbers = "04040404040",
                Message = "Message",
                StartTime = DateTime.Now.AddHours(2)
            };
            var actionResult = (ViewResult)controller.Create(model);

            Assert.That(actionResult.ViewName, Is.EqualTo("Create"));
        }

        [Test]
        public void CreateNewExcludeCoordinatorTopTenNoCoordinatorsSelected()
        {
            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            var trackingSession = MockRepository.GenerateMock<IDocumentSession>();

            var _store = new EmbeddableDocumentStore { RunInMemory = true };
            _store.Initialize();
            var mostRecentCoordinators = new List<CoordinatorTrackingData>
                {
                    new CoordinatorTrackingData
                        {
                            CoordinatorId = Guid.NewGuid(), 
                            MetaData = new SmsMetaData { Topic = "barry" }, 
                            CreationDateUtc = DateTime.Now.AddDays(-3),
                            MessageStatuses = new List<MessageSendingStatus> { new MessageSendingStatus { Status = MessageStatusTracking.CompletedSuccess }}
                        },
                    new CoordinatorTrackingData
                        {
                            CoordinatorId = Guid.NewGuid(), 
                            MetaData = new SmsMetaData { Topic = "simon" }, 
                            CreationDateUtc = DateTime.Now.AddDays(-4),
                            MessageStatuses = new List<MessageSendingStatus> { new MessageSendingStatus { Status = MessageStatusTracking.CompletedSuccess }}
                        }
                };

            var Session = _store.OpenSession();
            foreach (var coordinatorTrackingData in mostRecentCoordinators)
            {
                Session.Store(coordinatorTrackingData, coordinatorTrackingData.CoordinatorId.ToString());
            }
            Session.SaveChanges();

            ravenDocStore.Expect(r => r.GetStore().OpenSession("SmsTracking")).Return(Session);

            var controller = new CoordinatorController { ControllerContext = new ControllerContext(), RavenDocStore = ravenDocStore };
            var actionResult = (ViewResult)controller.Create();

            var coordinatorExcludeList = (actionResult.ViewData["CoordinatorExcludeList"] as List<SelectListItem>);
            Assert.That(coordinatorExcludeList.Count(), Is.EqualTo(2));
            Assert.IsFalse(coordinatorExcludeList[0].Selected);
            Assert.That(coordinatorExcludeList[0].Text.Contains("barry"));
            Assert.IsFalse(coordinatorExcludeList[1].Selected);
            Assert.That(coordinatorExcludeList[1].Text.Contains("simon"));

            ravenDocStore.VerifyAllExpectations();
            trackingSession.VerifyAllExpectations();
        }

        [Test]
        public void CreateEditExcludeCoordinatorTopTenNoCoordinatorsSelected()
        {
            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            var docSession = MockRepository.GenerateMock<IDocumentSession>();
            var docStore = MockRepository.GenerateMock<IDocumentStore>();
            var mapper = MockRepository.GenerateMock<ICoordinatorModelToMessageMapping>();
            var bus = MockRepository.GenerateMock<IBus>();

            ravenDocStore.Expect(d => d.GetStore()).Return(docStore);
            docStore.Expect(d => d.OpenSession("Configuration")).Return(docSession);
            docStore.Expect(d => d.OpenSession("SmsTracking")).Return(SmsTrackingSession);
            mapper
                .Expect(m => m.MapToTrickleSpacedByPeriod(Arg<CoordinatedSharedMessageModel>.Is.Anything, Arg<CountryCodeReplacement>.Is.Anything, Arg<List<string>>.Is.Anything))
                .Return(new TrickleSmsWithDefinedTimeBetweenEachMessage());
            bus.Expect(b => b.Send(Arg<TrickleSmsWithDefinedTimeBetweenEachMessage>.Is.Anything));

            docSession.Expect(d => d.Load<CountryCodeReplacement>("CountryCodeConfig")).Return(new CountryCodeReplacement());
            var model = new CoordinatedSharedMessageModel
            {
                Numbers = "04040404040",
                Message = "Message",
                StartTime = DateTime.Now.AddHours(2),
                CoordinatorsToExclude = new List<Guid>(),
                TimeSeparatorSeconds = 4,
                Topic = "frank"
            };

            var controller = new CoordinatorController { ControllerContext = new ControllerContext(), RavenDocStore = ravenDocStore, Mapper = mapper, Bus = bus };
            var actionResult = (RedirectToRouteResult)controller.Create(model);

            Assert.That(actionResult.RouteValues["action"], Is.EqualTo("Details"));

            ravenDocStore.VerifyAllExpectations();
        }
    }
}