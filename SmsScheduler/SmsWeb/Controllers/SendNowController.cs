using System;
using System.Linq;
using System.Web.Mvc;
using NServiceBus;
using SmsMessages.CommonData;
using SmsMessages.MessageSending;
using SmsMessages.MessageSending.Commands;
using SmsTracking;
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
        public ActionResult Create(FormCollection collection)
        {
            var model = ParseFormData(collection);
            var isValid = TryValidateModel(model);
            if(isValid)
            {
                model.MessageId = Guid.NewGuid();
                var sendOneMessageNow = new SendOneMessageNow
                {
                    CorrelationId = model.MessageId,
                    SmsData = new SmsData(model.Number, model.MessageBody), 
                    ConfirmationEmailAddress = model.ConfirmationEmail,
                    SmsMetaData = new SmsMetaData { Tags = model.Tags, Topic = model.Topic }
                };
                Bus.Send(sendOneMessageNow);
                return RedirectToAction("Details", "SendNow", new { requestId = model.MessageId.ToString()});
                //return View("Details", model);
            }
            ViewBag.tags = collection["tag"];
            return View("Create", model);
        }

        private SendNowModel ParseFormData(FormCollection formCollection)
        {
            var sendNowModel = new SendNowModel();
            if (hasValue(formCollection, "MessageBody"))
            {
                sendNowModel.MessageBody = formCollection["MessageBody"];
                if (sendNowModel.MessageBody.Length > 160)
                    sendNowModel.MessageBody = sendNowModel.MessageBody.Substring(0, 160);
            }
            if (hasValue(formCollection, "Number"))
                sendNowModel.Number = formCollection["Number"].Trim();
            if (hasValue(formCollection, "tag"))
                sendNowModel.Tags = formCollection["tag"].Split(',').ToList().Select(t => t.Trim()).ToList();

            sendNowModel.Topic = formCollection["Topic"];
            sendNowModel.ConfirmationEmail = formCollection["ConfirmationEmail"];
            return sendNowModel;
        }

        private bool hasValue(FormCollection formCollection, string key)
        {
            if (formCollection[key] != null && !string.IsNullOrWhiteSpace(formCollection[key]))
                return true;
            return false;
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
