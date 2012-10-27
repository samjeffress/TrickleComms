using System.Web.Mvc;
using NServiceBus;
using SmsMessages.CommonData;
using SmsMessages.MessageSending;
using SmsWeb.Models;

namespace SmsWeb.Controllers
{
    public class SendNowController : Controller
    {
        public IBus Bus { get; set; }

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
            if(isModelValid(sendNowModel))
            {
                Bus.Send(new SendOneMessageNow {SmsData = new SmsData(sendNowModel.Number, sendNowModel.MessageBody), ConfirmationEmailAddress = sendNowModel.ConfirmationEmail});
                return View("Details", sendNowModel);
            }
            return View("Create", sendNowModel);
        }

        private bool isModelValid(SendNowModel model)
        {
            var isValid = true;
            if (string.IsNullOrWhiteSpace(model.MessageBody))
                isValid = false;
            if (string.IsNullOrWhiteSpace(model.Number))
                isValid = false;
            if (string.IsNullOrWhiteSpace(model.ConfirmationEmail))
                isValid = false;
            return isValid;
        }
    }
}
