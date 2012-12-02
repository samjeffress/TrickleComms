using System;
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
        public ActionResult Create(SendNowModel sendNowModel)
        {
            var isValid = TryValidateModel(sendNowModel);
            if(isValid)
            {
                var sendOneMessageNow = new SendOneMessageNow {SmsData = new SmsData(sendNowModel.Number, sendNowModel.MessageBody), ConfirmationEmailAddress = sendNowModel.ConfirmationEmail};
                Bus.Send(sendOneMessageNow);
                return View("Details", sendNowModel);
            }
            return View("Create", sendNowModel);
        }

        public ActionResult Details(string requestId)
        {
            using (var session = RavenDocStore.GetStore().OpenSession())
            {
                var smsTrackingData = session.Load<SmsTrackingData>(requestId);
                if (smsTrackingData == null)
                    throw new NotImplementedException();
                //return View("DetailsNotCreated", scheduleId);
                return View("TrackingDetails", smsTrackingData);
            }
        }
    }
}
