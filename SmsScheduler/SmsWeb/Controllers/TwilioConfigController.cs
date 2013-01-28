using System.Web.Mvc;
using ConfigurationModels;

namespace SmsWeb.Controllers
{
    public class TwilioConfigController : Controller
    {
        public IRavenDocStore DocumentStore { get; set; }

        public ActionResult Index()
        {
            return View("Edit");
        }

        public ActionResult Create()
        {
            return View("Create");
        }

        [HttpPost]
        public ActionResult Create(TwilioConfiguration configuration)
        {
            var isValid = TryUpdateModel(configuration);
            if (isValid)
            {
                using (var session = DocumentStore.GetStore().OpenSession("Configuration"))
                {
                    var twilioConfiguration = session.Load<TwilioConfiguration>("TwilioConfig");
                    if (twilioConfiguration != null)
                    {
                        ModelState.AddModelError("General", "Twilio Config is already setup");
                        return View("Create", twilioConfiguration);
                    }
                    session.Store(configuration, "TwilioConfig");
                    session.SaveChanges();
                    return RedirectToAction("Details");
                }
            }
            return View("Create", configuration);
        }

        public ActionResult Edit()
        {
            using (var session = DocumentStore.GetStore().OpenSession("Configuration"))
            {
                var twilioConfiguration = session.Load<TwilioConfiguration>("TwilioConfig");
                return View("Edit", twilioConfiguration);
            }
        }

        public PartialViewResult EditAjax()
        {
            using (var session = DocumentStore.GetStore().OpenSession("Configuration"))
            {
                var twilioConfiguration = session.Load<TwilioConfiguration>("TwilioConfig");
                if (twilioConfiguration == null)
                    return PartialView("_TwilioConfigCreate");
                return PartialView("_TwilioConfigDetails", twilioConfiguration);
            }
        }

        [HttpPost]
        public ActionResult Edit(TwilioConfiguration configuration)
        {
            var isValid = TryUpdateModel(configuration);
            if (!isValid)
                return View("Edit", configuration);
            using (var session = DocumentStore.GetStore().OpenSession("Configuration"))
            {
                var twilioConfiguration = session.Load<TwilioConfiguration>("TwilioConfig");
                if (twilioConfiguration == null)
                {
                    session.Store(configuration, "TwilioConfig");
                }
                else
                {
                    twilioConfiguration.AccountSid = configuration.AccountSid;
                    twilioConfiguration.AuthToken = configuration.AuthToken;
                    twilioConfiguration.From = configuration.From;
                }
                session.SaveChanges();
                return RedirectToAction("Details");
            }
        }

        [HttpPost]
        public PartialViewResult EditAjax(TwilioConfiguration configuration)
        {
            var isValid = TryUpdateModel(configuration);
            if (!isValid)
                return PartialView("_TwilioConfigCreate", configuration);
            using (var session = DocumentStore.GetStore().OpenSession("Configuration"))
            {
                var twilioConfiguration = session.Load<TwilioConfiguration>("TwilioConfig");
                if (twilioConfiguration == null)
                {
                    session.Store(configuration, "TwilioConfig");
                }
                else
                {
                    twilioConfiguration.AccountSid = configuration.AccountSid;
                    twilioConfiguration.AuthToken = configuration.AuthToken;
                    twilioConfiguration.From = configuration.From;
                }
                session.SaveChanges();
                return PartialView("_TwilioConfigDetails", configuration);
            }
        }

        public ActionResult Details()
        {
            using (var session = DocumentStore.GetStore().OpenSession("Configuration"))
            {
                var twilioConfiguration = session.Load<TwilioConfiguration>("TwilioConfig");
                return View("Details", twilioConfiguration);
            }
        }
    }
}
