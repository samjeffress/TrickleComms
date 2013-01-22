using System;
using System.Linq;
using System.Web.Mvc;
using NServiceBus;
using SmsMessages.CommonData;
using SmsMessages.MessageSending.Commands;
using SmsTrackingModels;
using SmsWeb.Models;

namespace SmsWeb.Controllers
{
    public class SendNowController : Controller
    {
        public IBus Bus { get; set; }

        public IRavenDocStore RavenDocStore { get; set; }

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Create()
        {
            return View("Create");
        }

        [HttpPost]
        public ActionResult Create(SendNowModel model)
        {
            var isValid = TryValidateModel(model);
            if(isValid)
            {
                model.MessageId = Guid.NewGuid();
                if (model.MessageBody.Length > 160)
                    model.MessageBody = model.MessageBody.Substring(0, 160);
                var sendOneMessageNow = new SendOneMessageNow
                {
                    CorrelationId = model.MessageId,
                    SmsData = new SmsData(model.Number, model.MessageBody), 
                    ConfirmationEmailAddress = model.ConfirmationEmail,
                    SmsMetaData = new SmsMetaData
                    {
                        Tags = string.IsNullOrEmpty(model.Tags) ? null : model.Tags.Split(',').ToList().Select(t => t.Trim()).ToList(), 
                        Topic = model.Topic
                    }
                };
                Bus.Send(sendOneMessageNow);
                return RedirectToAction("Details", "SendNow", new { requestId = model.MessageId.ToString()});
            }
            return View("Create", model);
        }

        public ActionResult Details(string requestId)
        {
            using (var session = RavenDocStore.GetStore().OpenSession())
            {
                var smsTrackingData = session.Load<SmsTrackingData>(requestId);
                if (smsTrackingData == null)
                    return View("DetailsNotCreated", model: requestId);
                return View("TrackingDetails", smsTrackingData);
            }
        }
    }
}
