using System;
using System.Web.Mvc;
using ConfigurationModels;
using SmsTrackingModels;

namespace SmsWeb.Controllers
{
    public class HomeController : Controller
    {
        public IRavenDocStore RavenDocStore { get; set; }

        public ActionResult Index()
        {
            using (var session = RavenDocStore.GetStore().OpenSession("Configuration"))
            {
                var twilioConfiguration = session.Load<TwilioConfiguration>("TwilioConfig");
                var mailgunConfiguration = session.Load<MailgunConfiguration>("MailgunConfig");

                if (twilioConfiguration == null || mailgunConfiguration == null)
                {
                    return View("IndexConfigNotSet");
                }
            }
            return View("Index");
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your app description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public ActionResult Configuration()
        {
            return View("ConfigMenu");
        }

        [HttpPost]
        public ActionResult Search(string id)
        {
            Guid docId;
            Guid.TryParse(id, out docId);
            using (var session = RavenDocStore.GetStore().OpenSession())
            {
                var trackingData = session.Load<object>(docId.ToString());
                if (trackingData != null)
                {
                    if (trackingData is CoordinatorTrackingData)
                        return RedirectToAction("Details", "Coordinator", new {coordinatorId = id});
                    if (trackingData is ScheduleTrackingData)
                        return RedirectToAction("Details", "Schedule", new {scheduleId = id});
                    if (trackingData is SmsTrackingData)
                        return RedirectToAction("Details", "SendNow", new {requestId = id});
                    throw new Exception("Type not recognised");
                }
            }
            return View("NoResults", (object)id);
        }
    }
}
