using System;
using System.Collections.Generic;
using System.Web.Mvc;
using ConfigurationModels;
using Raven.Client.Linq;
using SmsTrackingModels;
using System.Linq;
using SmsWeb.Models;

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

            if (Guid.TryParse(id, out docId))
            {
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
            }
            else
            {
                using (var session = RavenDocStore.GetStore().OpenSession())
                {
                    var reduceResults = session.Query<PhoneNumberInCoordinatedMessages.ReduceResult, PhoneNumberInCoordinatedMessages>()
                        .Where(p => p.PhoneNumber.StartsWith(id))
                        .Select(p => new SearchByNumberResult
                            {
                                CoordinatorId = p.CoordinatorId,
                                PhoneNumber = p.PhoneNumber,
                                SendingDate = p.SendingDate,
                                Status = p.Status,
                                Topic = p.Topic,

                            })
                        .ToList();
                    return View("Results", reduceResults);
                }
            }
            return View("NoResults", (object)id);
        }
    }
}
