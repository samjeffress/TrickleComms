using System.Web.Mvc;
using ConfigurationModels;

namespace SmsWeb.Controllers
{
    public class MailgunConfigController : Controller
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
        public ActionResult Create(MailgunConfiguration configuration)
        {
            var isValid = TryUpdateModel(configuration);
            if (isValid)
            {
                using (var session = DocumentStore.GetStore().OpenSession("Configuration"))
                {
                    var mailgunConfiguration = session.Load<MailgunConfiguration>("MailgunConfig");
                    if (mailgunConfiguration != null)
                    {
                        ModelState.AddModelError("General", "Mailgun Config is already setup");
                        return View("Create", mailgunConfiguration);
                    }
                    session.Store(configuration, "MailgunConfig");
                    session.SaveChanges();
                    return View("Details", configuration);
                }
            }
            return View("Create", configuration);
        }

        public ActionResult Edit()
        {
            using (var session = DocumentStore.GetStore().OpenSession("Configuration"))
            {
                var mailgunConfiguration = session.Load<MailgunConfiguration>("MailgunConfig");
                return View("Edit", mailgunConfiguration);
            }
        }

        [HttpPost]
        public ActionResult Edit(MailgunConfiguration configuration)
        {
            var isValid = TryUpdateModel(configuration);
            if (!isValid)
                return View("Edit", configuration);
            using (var session = DocumentStore.GetStore().OpenSession("Configuration"))
            {
                var mailgunConfiguration = session.Load<MailgunConfiguration>("MailgunConfig");
                if (mailgunConfiguration == null)
                {
                    session.Store(configuration, "MailgunConfig");
                }
                else
                {
                    mailgunConfiguration = configuration;
                }
                session.SaveChanges();
                return View("Edit", mailgunConfiguration);
            }
        }
    }
}