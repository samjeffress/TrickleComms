using System;
using System.Web.Mvc;
using NUnit.Framework;
using Raven.Client;
using Raven.Client.Embedded;
using Rhino.Mocks;
using SmsTracking;
using SmsWeb.Controllers;

namespace SmsWebTests
{
    [TestFixture]
    public class SearchTestFixture : RavenTestBase
    {
        private readonly Guid _coordinatorId = Guid.NewGuid();
        private readonly Guid _scheduleId = Guid.NewGuid();
        private readonly Guid _smsId = Guid.NewGuid();
        readonly CoordinatorTrackingData _coordinatorTrackingData = new CoordinatorTrackingData();
        readonly ScheduleTrackingData _scheduleTrackingData = new ScheduleTrackingData();
        readonly SmsTrackingData _smsTrackingData = new SmsTrackingData();

        [Test]        
        public void FoundCoordinator()
        {
            var ravenDocStore = MockRepository.GenerateMock<SmsWeb.IRavenDocStore>();
            ravenDocStore.Expect(r => r.GetStore()).Return(DocumentStore);
            var controller = new HomeController { RavenDocStore = ravenDocStore };
            var actionResult = controller.Search(_coordinatorId.ToString()) as RedirectToRouteResult;

            Assert.That(actionResult.RouteValues["controller"], Is.EqualTo("Coordinator"));
            Assert.That(actionResult.RouteValues["action"], Is.EqualTo("Details"));
            Assert.That(actionResult.RouteValues["coordinatorid"], Is.EqualTo(_coordinatorId.ToString()));
        }

        [Test]
        public void FoundSchedule()
        {
            var ravenDocStore = MockRepository.GenerateMock<SmsWeb.IRavenDocStore>();
            ravenDocStore.Expect(r => r.GetStore()).Return(DocumentStore);
            var controller = new HomeController { RavenDocStore = ravenDocStore };
            var actionResult = controller.Search(_scheduleId.ToString()) as RedirectToRouteResult;

            Assert.That(actionResult.RouteValues["controller"], Is.EqualTo("Schedule"));
            Assert.That(actionResult.RouteValues["action"], Is.EqualTo("Details"));
            Assert.That(actionResult.RouteValues["scheduleid"], Is.EqualTo(_scheduleId.ToString()));
        }

        [Test]
        public void FoundSentMessage()
        {
            var ravenDocStore = MockRepository.GenerateMock<SmsWeb.IRavenDocStore>();
            ravenDocStore.Expect(r => r.GetStore()).Return(DocumentStore);
            var controller = new HomeController { RavenDocStore = ravenDocStore };
            var actionResult = controller.Search(_smsId.ToString()) as RedirectToRouteResult;

            Assert.That(actionResult.RouteValues["controller"], Is.EqualTo("SendNow"));
            Assert.That(actionResult.RouteValues["action"], Is.EqualTo("Details"));
            Assert.That(actionResult.RouteValues["requestId"], Is.EqualTo(_smsId.ToString()));
        }

        [Test]
        public void FoundNothing()
        {
            var ravenDocStore = MockRepository.GenerateMock<SmsWeb.IRavenDocStore>();
            ravenDocStore.Expect(r => r.GetStore()).Return(DocumentStore);
            var controller = new HomeController { RavenDocStore = ravenDocStore };
            var actionResult = controller.Search(Guid.NewGuid().ToString()) as ViewResult;

            Assert.That(actionResult.ViewName, Is.EqualTo("NoResults"));
        }

        [SetUp]
        public void Setup()
        {
            using (var session = base.DocumentStore.OpenSession())
            {
                session.Store(_coordinatorTrackingData, _coordinatorId.ToString());
                session.Store(_scheduleTrackingData, _scheduleId.ToString());
                session.Store(_smsTrackingData, _smsId.ToString());
                session.SaveChanges();
            }
        }
    }
    public class RavenTestBase
    {
        protected IDocumentStore DocumentStore;

        [SetUp]
        public void Setup()
        {
            DocumentStore = new EmbeddableDocumentStore { RunInMemory = true }.Initialize();
        }

        [TearDown]
        public void Teardown()
        {
            DocumentStore.Dispose();
        }
    }
}