using System;
using System.Web.Mvc;
using SmsTrackingModels;

namespace SmsWeb.Controllers
{
    public class HomeController : Controller
    {
        public IRavenDocStore RavenDocStore { get; set; }

        public ActionResult Index()
        {
            ViewBag.Message = "Modify this template to jump-start your ASP.NET MVC application.";

            return View();
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
