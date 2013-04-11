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
                    return RedirectToAction("Details");
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

        public PartialViewResult DetailsAjax()
        {
            using (var session = DocumentStore.GetStore().OpenSession("Configuration"))
            {
                var mailgunConfiguration = session.Load<MailgunConfiguration>("MailgunConfig");
                if (mailgunConfiguration == null)
                    return PartialView("_MailgunConfigEdit");
                return PartialView("_MailgunConfigDetails", mailgunConfiguration);
            }
        }

        public PartialViewResult EditAjax()
        {
            using (var session = DocumentStore.GetStore().OpenSession("Configuration"))
            {
                var mailgunConfiguration = session.Load<MailgunConfiguration>("MailgunConfig");
                if (mailgunConfiguration == null)
                    return PartialView("_MailgunConfigEdit");
                return PartialView("_MailgunConfigEdit", mailgunConfiguration);
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
                    mailgunConfiguration.ApiKey = configuration.ApiKey;
                    mailgunConfiguration.DefaultFrom = configuration.DefaultFrom;
                    mailgunConfiguration.DomainName = configuration.DomainName;
                }
                session.SaveChanges();
                return RedirectToAction("Details");
            }
        }

        [HttpPost]
        public PartialViewResult EditAjax(MailgunConfiguration configuration)
        {
            var isValid = TryUpdateModel(configuration);
            if (!isValid)
                return PartialView("_MailgunConfigEdit", configuration);
            using (var session = DocumentStore.GetStore().OpenSession("Configuration"))
            {
                var mailgunConfiguration = session.Load<MailgunConfiguration>("MailgunConfig");
                if (mailgunConfiguration == null)
                {
                    session.Store(configuration, "MailgunConfig");
                }
                else
                {
                    mailgunConfiguration.ApiKey = configuration.ApiKey;
                    mailgunConfiguration.DefaultFrom = configuration.DefaultFrom;
                    mailgunConfiguration.DomainName = configuration.DomainName;
                }
                session.SaveChanges();
                return PartialView("_MailgunConfigDetails", configuration);
            }
        }

        public ActionResult Details()
        {
            using (var session = DocumentStore.GetStore().OpenSession("Configuration"))
            {
                var mailgunConfiguration = session.Load<MailgunConfiguration>("MailgunConfig");
                return View("Details", mailgunConfiguration);
            }
        }
    }
}