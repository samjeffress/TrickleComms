using System;
using System.Collections.Generic;
using System.Web.Mvc;
using ConfigurationModels;
using Raven.Client.Linq;
using SmsTrackingModels;
using System.Linq;
using System.Reflection;
using SmsTrackingModels.RavenIndexs;
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
                var smsProviderConfiguration = session.Load<SmsProviderConfiguration>("SmsProviderConfiguration");
                var emailProviderConfiguration = session.Load<EmailProviderConfiguration>("EmailProviderConfiguration");

                if (smsProviderConfiguration.SmsProvider == SmsProvider.NoSmsFunctionality || emailProviderConfiguration.EmailProvider == EmailProvider.NoEmailFunctionality)
                    return View("IndexConfigNotSet");

                Assembly asm = typeof (SmsProviderConfiguration).Assembly;
                var expectedEmailType = "ConfigurationModels.Providers."+emailProviderConfiguration.EmailProvider.ToString() + "Configuration";
                Type emailType = asm.GetType(expectedEmailType);

                var expectedSmsType = "ConfigurationModels.Providers"+smsProviderConfiguration.SmsProvider.ToString() + "Configuration";
                Type smsType = asm.GetType(expectedSmsType);

                var emailProvider = session.Load<dynamic>(emailProviderConfiguration.EmailProvider.ToString() + "Config");
                var smsProider = session.Load<dynamic>(smsProviderConfiguration.SmsProvider.ToString() + "Config");

                // change this to an &&
                if (emailProvider == null || smsProider == null)
                    return View("IndexConfigNotSet");
                else
                    return RedirectToAction("Create", "Coordinator");
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
                        throw new Exception("Type not recognised");
                    }
                }
            }
            else
            {
                using (var session = RavenDocStore.GetStore().OpenSession())
                {
                    var list = session.Query<SmsActioned.Result, SmsActioned>()
                        .Where(p => p.Number.EndsWith(id))
                        .Select(p => new SearchByNumberResult
                        {
                            PhoneNumber = p.Number,
                            SendingDate = p.ActionTime,
                            Status = p.Status,
                            Topic = p.Topic
                        })
                        .ToList();

                    return View("Results", list);

                    //var reduceResults = session.Query<PhoneNumberInCoordinatedMessages.ReduceResult, PhoneNumberInCoordinatedMessages>()
                    //    .Where(p => p.PhoneNumber.StartsWith(id))
                    //    .Select(p => new SearchByNumberResult
                    //        {
                    //            CoordinatorId = p.CoordinatorId,
                    //            PhoneNumber = p.PhoneNumber,
                    //            SendingDate = p.SendingDate,
                    //            Status = p.Status,
                    //            Topic = p.Topic,

                    //        })
                    //    .ToList();
                    //return View("Results", reduceResults);
                }
            }
            return View("NoResults", (object)id);
        }
    }
}
