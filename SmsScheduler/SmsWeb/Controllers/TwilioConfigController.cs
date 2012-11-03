using System.Web.Mvc;
using SmsCoordinator;

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
                using (var session = DocumentStore.GetStore().OpenSession("TwilioConfiguration"))
                {
                    var twilioConfiguration = session.Load<TwilioConfiguration>("TwilioConfig");
                    if (twilioConfiguration != null)
                    {
                        ModelState.AddModelError("General", "Twilio Config is already setup");
                        return View("Create", twilioConfiguration);
                    }
                    session.Store(configuration, "TwilioConfig");
                    session.SaveChanges();
                }

                return View("Details");
            }
            return View("Create", configuration);
        }

        public ActionResult Edit()
        {
            using (var session = DocumentStore.GetStore().OpenSession("TwilioConfiguration"))
            {
                var twilioConfiguration = session.Load<TwilioConfiguration>("TwilioConfig");
                return View("Edit", twilioConfiguration);
            }
        }

        [HttpPost]
        public ActionResult Edit(TwilioConfiguration configuration)
        {
            var isValid = TryUpdateModel(configuration);
            if (!isValid)
                return View("Edit", configuration);
            using (var session = DocumentStore.GetStore().OpenSession("TwilioConfiguration"))
            {
                var twilioConfiguration = session.Load<TwilioConfiguration>("TwilioConfig");
                if (twilioConfiguration == null)
                {
                    session.Store(configuration, "TwilioConfig");
                }
                else
                {
                    twilioConfiguration = configuration;
                }
                session.SaveChanges();
                return View("Edit", twilioConfiguration);
            }
        }
    }
}
