using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ConfigurationModels;
using NServiceBus;
using NUnit.Framework;
using Raven.Client;
using Raven.Client.Embedded;
using Raven.Client.Indexes;
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
            IndexCreation.CreateIndexes(typeof(ScheduleMessagesInCoordinatorIndex).Assembly, _store);
            Top1CoordinatorId = Guid.NewGuid();
            var mostRecentCoordinators = new List<CoordinatorTrackingData>
                {
                    new CoordinatorTrackingData(new List<MessageSendingStatus>())
                        {
                            CoordinatorId = Top1CoordinatorId, 
                            MetaData = new SmsMetaData { Topic = "barry" }, 
                            CreationDateUtc = DateTime.Now.AddDays(-3),
                        },
                };
            var scheduleTrackingData = new ScheduleTrackingData {CoordinatorId = Top1CoordinatorId, MessageStatus = MessageStatus.Sent};

            SmsTrackingSession = _store.OpenSession();
            foreach (var coordinatorTrackingData in mostRecentCoordinators)
            {
                SmsTrackingSession.Store(coordinatorTrackingData, coordinatorTrackingData.CoordinatorId.ToString());
            }
            SmsTrackingSession.Store(scheduleTrackingData, Guid.NewGuid().ToString());
            SmsTrackingSession.SaveChanges();
        }

        [Test]        
        public void CreateSeparatedByTimeSpanReturnsDetails()
        {
            var model = new CoordinatedSharedMessageModel
            {
                Numbers= "04040404040, 04040402",
                Message = "Message",
                StartTime = DateTime.Now.AddHours(2),
                TimeSeparatorSeconds = 5000,
                Tags = "tag1, tag2",
                Topic = "New Feature!",
                UserTimeZone = "Australia/Sydney"
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
        public void CreateSendAllAtOnceReturnsDetails()
        {
            var model = new CoordinatedSharedMessageModel
            {
                Numbers= "04040404040",
                Message = "Message",
                StartTime = DateTime.Now.AddHours(2),
                SendAllAtOnce = true,
                Tags = "tag1, tag2",
                Topic = "New Feature!",
                UserTimeZone = "Australia/Sydney"
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
                .Expect(m => m.MapToSendAllAtOnce(Arg<CoordinatedSharedMessageModel>.Is.Anything, Arg<CountryCodeReplacement>.Is.Anything, Arg<List<string>>.Is.Anything))
                .Return(new SendAllMessagesAtOnce());
            var trickleMessage = new SendAllMessagesAtOnce();
            bus.Expect(b => b.Send(Arg<SendAllMessagesAtOnce>.Is.NotNull))
                .WhenCalled(i => trickleMessage = (SendAllMessagesAtOnce) ((object[]) (i.Arguments[0]))[0]);

            var controller = new CoordinatorController { ControllerContext = new ControllerContext(), Bus = bus, Mapper = mapper, RavenDocStore = ravenDocStore };
            var actionResult = (RedirectToRouteResult)controller.Create(model);

            Assert.That(actionResult.RouteValues["action"], Is.EqualTo("Details"));
            Assert.That(trickleMessage.CoordinatorId, Is.Not.EqualTo(Guid.Empty));

            bus.VerifyAllExpectations();
            mapper.VerifyAllExpectations();
        }

        [Test]
        public void CreateOverTimespanReturnsDetails()
        {
            var model = new CoordinatedSharedMessageModel
            {
                Numbers = "04040404040, lskadfjlasdk",
                Message = "Message",
                StartTime = DateTime.Now.AddHours(2),
                SendAllBy = DateTime.Now.AddHours(3),
                Topic = "frank",
                UserTimeZone = "Australia/Sydney"
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
        public void CreateOverTimespanLongMessageIsShortenedReturnsDetails()
        {
            var model = new CoordinatedSharedMessageModel
            {
                Numbers = "04040404040, 0920939",
                Message = "asfdkjadfskl asflkj;faskjf;aslkjf;lasdkjfaslkfjas;lkfjslkfjas;lkfjsalkfjas;fklasj;flksdjf;lkasjflskdjflkasjflksjlk lskaf jlsk fdaskl dflksjfalk sflkj sfkl jlkjs flkj skjkj sadflkjsaflj",
                StartTime = DateTime.Now.AddHours(2),
                SendAllBy = DateTime.Now.AddHours(3),
                Topic = "frank",
                UserTimeZone = "Australia/Sydney"
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
        public void CreateExcludePreviousCoordinatorMessagesRemovesMatchingNumbers()
        {
            var CoordinatorToExclude = Guid.NewGuid();
            var model = new CoordinatedSharedMessageModel
            {
                Numbers = "04040404040, 1, 2, 3",
                Message = "asfdkjadfskl asflkj;faskjf;aslkjf;lasdkjfaslkfjas;lkfjslkfjas;lkfjsalkfjas;fklasj;flksdjf;lkasjflskdjflkasjflksjlk lskaf jlsk fdaskl dflksjfalk sflkj sfkl jlkjs flkj skjkj sadflkjsaflj",
                StartTime = DateTime.Now.AddHours(2),
                SendAllBy = DateTime.Now.AddHours(3),
                CoordinatorsToExclude = new List<Guid> { CoordinatorToExclude },
                Topic = "frank",
                UserTimeZone = "Australia/Sydney"
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
            var previousCoordinatorToExclude = new CoordinatorTrackingData(new List<MessageSendingStatus> { new MessageSendingStatus { Number = "04040404040" }, new MessageSendingStatus { Number = "1" } });
            trackingSession.Expect(d => d.Load<CoordinatorTrackingData>(CoordinatorToExclude.ToString())).Return(
                previousCoordinatorToExclude);

            var coordinatorMessage = new CoordinatedSharedMessageModel();
            var excludeList = previousCoordinatorToExclude.GetListOfCoordinatedSchedules(ravenDocStore.GetStore()).Select(s => s.Number).ToList();
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
        public void CreaetExcludeMultiplePreviousCoordinatorMessagesRemovesMatchingNumbers_TrickleBetweenDates()
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
                Topic = "frank",
                UserTimeZone = "Australia/Sydney"
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
            var previousCoordinatorToExclude1 = new CoordinatorTrackingData(new List<MessageSendingStatus> { new MessageSendingStatus { Number = "04040404040" }, new MessageSendingStatus { Number = "1" } });
            trackingSession.Expect(d => d.Load<CoordinatorTrackingData>(CoordinatorToExclude1.ToString())).Return(
                previousCoordinatorToExclude1);            
            var previousCoordinatorToExclude2 = new CoordinatorTrackingData(new List<MessageSendingStatus> { new MessageSendingStatus { Number = "7" } });
            trackingSession.Expect(d => d.Load<CoordinatorTrackingData>(CoordinatorToExclude2.ToString())).Return(
                previousCoordinatorToExclude2);

            var coordinatorMessage = new CoordinatedSharedMessageModel();
            var excludeList1 = previousCoordinatorToExclude1.GetListOfCoordinatedSchedules(ravenDocStore.GetStore()).Select(s => s.Number).ToList();
            var excludeList2 = previousCoordinatorToExclude2.GetListOfCoordinatedSchedules(ravenDocStore.GetStore()).Select(s => s.Number).ToList();

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
        public void CreateSingleNumber_UseSendAllNow()
        {
            var model = new CoordinatedSharedMessageModel
            {
                Numbers = "04040404040",
                Message = "asfdkjadfskl asflkj;faskjf;aslkjf;lasdkjfaslkfjas;lkfjslkfjas;lkfjsalkfjas;fklasj;flksdjf;lkasjflskdjflkasjflksjlk lskaf jlsk fdaskl dflksjfalk sflkj sfkl jlkjs flkj skjkj sadflkjsaflj",
                StartTime = DateTime.Now.AddHours(2),
                Topic = "frank",
                UserTimeZone = "Australia/Sydney"
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

            var coordinatorMessage = new CoordinatedSharedMessageModel();
            
            List<string> excludeList = null;
            mapper
                .Expect(m => m.MapToSendAllAtOnce(Arg<CoordinatedSharedMessageModel>.Is.Anything, Arg<CountryCodeReplacement>.Is.Anything, Arg<List<string>>.Is.Anything))
                .Return(new SendAllMessagesAtOnce())
                .WhenCalled(t => coordinatorMessage = (CoordinatedSharedMessageModel)(t.Arguments[0]))
                .WhenCalled(t => excludeList = (List<string>)(t.Arguments[2]));
            bus.Expect(b => b.Send(Arg<TrickleSmsOverCalculatedIntervalsBetweenSetDates>.Is.NotNull));

            var controller = new CoordinatorController { ControllerContext = new ControllerContext(), Bus = bus, Mapper = mapper, RavenDocStore = ravenDocStore };
            var actionResult = (RedirectToRouteResult)controller.Create(model);

            Assert.That(actionResult.RouteValues["action"], Is.EqualTo("Details"));
            Assert.That(coordinatorMessage.Message, Is.EqualTo(model.Message.Substring(0, 160)));

            bus.VerifyAllExpectations();
            mapper.VerifyAllExpectations();
            configSession.VerifyAllExpectations();
            trackingSession.VerifyAllExpectations();
        }

        [Test]
        public void CreateExcludeMultiplePreviousCoordinatorMessagesRemovesMatchingNumbers_TrickleMessageSpaceDefined()
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
                Topic = "frank",
                UserTimeZone = "Australia/Sydney"
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
            var previousCoordinatorToExclude1 = new CoordinatorTrackingData(new List<MessageSendingStatus> { new MessageSendingStatus { Number = "04040404040" }, new MessageSendingStatus { Number = "1" } });
            trackingSession.Expect(d => d.Load<CoordinatorTrackingData>(CoordinatorToExclude1.ToString())).Return(previousCoordinatorToExclude1);            
            var previousCoordinatorToExclude2 = new CoordinatorTrackingData(new List<MessageSendingStatus> { new MessageSendingStatus { Number = "7" } });
            trackingSession.Expect(d => d.Load<CoordinatorTrackingData>(CoordinatorToExclude2.ToString())).Return(previousCoordinatorToExclude2);

            var coordinatorMessage = new CoordinatedSharedMessageModel();
            var excludeList1 = previousCoordinatorToExclude1.GetListOfCoordinatedSchedules(ravenDocStore.GetStore()).Select(s => s.Number).ToList();
            var excludeList2 = previousCoordinatorToExclude2.GetListOfCoordinatedSchedules(ravenDocStore.GetStore()).Select(s => s.Number).ToList();

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
        public void CreateContainsNoNumbersError()
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
        public void CreateContainsNoMessagesError()
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
        public void CreateErrorWithSelectedCoordinatorsToExclude()
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
        public void CreateTimeInPastError()
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
        public void CreateNoTopicError()
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
        public void CreateTimeSeparatorNotDefinedError()
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
                    new CoordinatorTrackingData (new List<MessageSendingStatus> { new MessageSendingStatus { Status = MessageStatusTracking.CompletedSuccess }})
                        {
                            CoordinatorId = Guid.NewGuid(), 
                            MetaData = new SmsMetaData { Topic = "barry" }, 
                            CreationDateUtc = DateTime.Now.AddDays(-3),
                        },
                    new CoordinatorTrackingData (new List<MessageSendingStatus> { new MessageSendingStatus { Status = MessageStatusTracking.CompletedSuccess }})
                        {
                            CoordinatorId = Guid.NewGuid(), 
                            MetaData = new SmsMetaData { Topic = "simon" }, 
                            CreationDateUtc = DateTime.Now.AddDays(-4),
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
                Numbers = "04040404040, 3984938",
                Message = "Message",
                StartTime = DateTime.Now.AddHours(2),
                CoordinatorsToExclude = new List<Guid>(),
                TimeSeparatorSeconds = 4,
                Topic = "frank",
                UserTimeZone = "Australia/Sydney"
            };

            var controller = new CoordinatorController { ControllerContext = new ControllerContext(), RavenDocStore = ravenDocStore, Mapper = mapper, Bus = bus };
            var actionResult = (RedirectToRouteResult)controller.Create(model);

            Assert.That(actionResult.RouteValues["action"], Is.EqualTo("Details"));

            ravenDocStore.VerifyAllExpectations();
        }

        [Test]
        public void RescheduleWithResumeTime()
        {
            var bus = MockRepository.GenerateMock<IBus>();
            var dateTimeMapper = MockRepository.GenerateMock<IDateTimeUtcFromOlsenMapping>();
            var context = MockRepository.GenerateMock<ControllerContext>();
            var httpSessionStateBase = MockRepository.GenerateStub<HttpSessionStateBase>();
            var coordinatorController = new CoordinatorController { Bus = bus, DateTimeOlsenMapping = dateTimeMapper, ControllerContext = context };

            var timeToResumeAt = DateTime.Now;
            var coordinatorId = Guid.NewGuid();

            var collection = new FormCollection
                {
                    {"CoordinatorId", coordinatorId.ToString()},
                    {"timeToResume", DateTime.Now.AddMinutes(20).ToString()},
                    {"finishTime", string.Empty},
                    {"UserTimeZone", "MadeUpLand"}
                };
            ResumeTrickledMessages resumeMessage = null;
            bus
                .Expect(b => b.Send(Arg<ResumeTrickledMessages>.Is.Anything))
                .WhenCalled(b => resumeMessage = (ResumeTrickledMessages) ((object[])b.Arguments[0])[0]);
            
            dateTimeMapper
                .Expect(d => d.DateTimeWithOlsenZoneToUtc(DateTime.Parse(collection["timeToResume"]), collection["UserTimeZone"]))
                .Return(timeToResumeAt);

            context.Expect(c => c.HttpContext.Session).Return(httpSessionStateBase);
            
            var result = (RedirectToRouteResult)coordinatorController.Resume(collection);

            Assert.That(result.RouteValues["action"], Is.EqualTo("Details"));
            
            Assert.That(resumeMessage.CoordinatorId, Is.EqualTo(coordinatorId));
            Assert.That(resumeMessage.ResumeTimeUtc, Is.EqualTo(timeToResumeAt));

            bus.VerifyAllExpectations();
        }

        [Test]
        public void RescheduleWithStartAndFinishTime()
        {
            var bus = MockRepository.GenerateMock<IBus>();
            var dateTimeMapper = MockRepository.GenerateMock<IDateTimeUtcFromOlsenMapping>();
            var context = MockRepository.GenerateMock<ControllerContext>();
            var httpSessionStateBase = MockRepository.GenerateStub<HttpSessionStateBase>();
            var coordinatorController = new CoordinatorController { Bus = bus, DateTimeOlsenMapping = dateTimeMapper, ControllerContext = context };

            var timeToResume = DateTime.Now;
            var timeToFinish = DateTime.Now.AddMinutes(44);
            var coordinatorId = Guid.NewGuid();

            var collection = new FormCollection
                {
                    {"CoordinatorId", coordinatorId.ToString()},
                    {"timeToResume", DateTime.Now.AddMinutes(20).ToString()},
                    {"finishTime", DateTime.Now.AddMinutes(30).ToString()},
                    {"UserTimeZone", "MadeUpLand"}
                };
            RescheduleTrickledMessages rescheduleMessage = null;
            bus
                .Expect(b => b.Send(Arg<RescheduleTrickledMessages>.Is.Anything))
                .WhenCalled(b => rescheduleMessage = (RescheduleTrickledMessages)((object[])b.Arguments[0])[0]);
            
            dateTimeMapper
                .Expect(d => d.DateTimeWithOlsenZoneToUtc(DateTime.Parse(collection["timeToResume"]), collection["UserTimeZone"]))
                .Return(timeToResume);

            dateTimeMapper
                .Expect(d => d.DateTimeWithOlsenZoneToUtc(DateTime.Parse(collection["finishTime"]), collection["UserTimeZone"]))
                .Return(timeToFinish);

            context.Expect(c => c.HttpContext.Session).Return(httpSessionStateBase);
            
            var result = (RedirectToRouteResult)coordinatorController.Resume(collection);

            Assert.That(result.RouteValues["action"], Is.EqualTo("Details"));

            Assert.That(rescheduleMessage.CoordinatorId, Is.EqualTo(coordinatorId));
            Assert.That(rescheduleMessage.ResumeTimeUtc, Is.EqualTo(timeToResume));
            Assert.That(rescheduleMessage.FinishTimeUtc, Is.EqualTo(timeToFinish));
            
            bus.VerifyAllExpectations();
        }

        [Test]
        public void ResumeWithStartTimeAndInvalidFinishTimeReturnsError()
        {
            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            ravenDocStore.Expect(r => r.GetStore().OpenSession()).Return(SmsTrackingSession);

            var bus = MockRepository.GenerateMock<IBus>();
            var dateTimeMapper = MockRepository.GenerateMock<IDateTimeUtcFromOlsenMapping>();
            var context = MockRepository.GenerateMock<ControllerContext>();
            var httpSessionStateBase = MockRepository.GenerateStub<HttpSessionStateBase>();
            var coordinatorController = new CoordinatorController { Bus = bus, DateTimeOlsenMapping = dateTimeMapper, ControllerContext = context, RavenDocStore = ravenDocStore };

            var coordinatorId = Top1CoordinatorId;
            context.Expect(c => c.HttpContext.Session).Return(httpSessionStateBase);
            var collection = new FormCollection
                {
                    {"CoordinatorId", coordinatorId.ToString()},
                    {"timeToResume", DateTime.Now.AddMinutes(20).ToString()},
                    {"finishTime", DateTime.Now.AddMinutes(10).ToString()},
                    {"UserTimeZone", "MadeUpLand"}
                };

            var result = (ViewResult)coordinatorController.Resume(collection);

            // assert that there are viewdata error state set
            var modelStateDictionary = result.ViewData.ModelState;
            Assert.That(modelStateDictionary["finishTime"].Errors[0].ErrorMessage, Is.EqualTo("Finish time must be after time to resume"));

            bus.VerifyAllExpectations();
        }

        [Test]
        public void ResumeWithInvalidStartTimeReturnsError()
        {
            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            ravenDocStore.Expect(r => r.GetStore().OpenSession()).Return(SmsTrackingSession);

            var bus = MockRepository.GenerateMock<IBus>();
            var dateTimeMapper = MockRepository.GenerateMock<IDateTimeUtcFromOlsenMapping>();
            var context = MockRepository.GenerateMock<ControllerContext>();
            var httpSessionStateBase = MockRepository.GenerateStub<HttpSessionStateBase>();
            var coordinatorController = new CoordinatorController { Bus = bus, DateTimeOlsenMapping = dateTimeMapper, ControllerContext = context, RavenDocStore = ravenDocStore };

            var coordinatorId = Top1CoordinatorId;
            context.Expect(c => c.HttpContext.Session).Return(httpSessionStateBase);
            var collection = new FormCollection
                {
                    {"CoordinatorId", coordinatorId.ToString()},
                    {"timeToResume", DateTime.Now.AddMinutes(-20).ToString()},
                    {"UserTimeZone", "MadeUpLand"}
                };

            var result = (ViewResult)coordinatorController.Resume(collection);

            // assert that there are viewdata error state set
            var modelStateDictionary = result.ViewData.ModelState;
            Assert.That(modelStateDictionary["timeToResume"].Errors[0].ErrorMessage, Is.EqualTo("Time to resume must be in the future"));

            bus.VerifyAllExpectations();
        }

        [Test]
        public void ResumeWithEmptyStartTimeReturnsError()
        {
            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            ravenDocStore.Expect(r => r.GetStore().OpenSession()).Return(SmsTrackingSession);

            var bus = MockRepository.GenerateMock<IBus>();
            var dateTimeMapper = MockRepository.GenerateMock<IDateTimeUtcFromOlsenMapping>();
            var context = MockRepository.GenerateMock<ControllerContext>();
            var httpSessionStateBase = MockRepository.GenerateStub<HttpSessionStateBase>();
            var coordinatorController = new CoordinatorController { Bus = bus, DateTimeOlsenMapping = dateTimeMapper, ControllerContext = context, RavenDocStore = ravenDocStore };

            var coordinatorId = Top1CoordinatorId;
            context.Expect(c => c.HttpContext.Session).Return(httpSessionStateBase);
            var collection = new FormCollection
                {
                    {"CoordinatorId", coordinatorId.ToString()},
                    {"timeToResume", string.Empty },
                    {"UserTimeZone", "MadeUpLand"}
                };

            var result = (ViewResult)coordinatorController.Resume(collection);

            // assert that there are viewdata error state set
            var modelStateDictionary = result.ViewData.ModelState;
            Assert.That(modelStateDictionary["timeToResume"].Errors[0].ErrorMessage, Is.EqualTo("Time to resume must be set"));

            bus.VerifyAllExpectations();
        }
    }
}