using System.Web.Mvc;
using SmsWeb.Models;

namespace SmsWeb.Controllers
{
    public class SendNowController : Controller
    {
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
                // do stuff
            }
            return View("Create", sendNowModel);
        }

        private bool isModelValid(SendNowModel model)
        {
            return false;
        }
    }
}
