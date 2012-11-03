using System;
using System.Web.Mvc;
using NServiceBus;
using SmsWeb.Models;

namespace SmsWeb.Controllers
{
    public class ScheduleController : Controller
    {
        public IBus Bus { get; set; }

        public ActionResult Create()
        {
            return View("Create");
        }

        [HttpPost]
        public ActionResult Create(ScheduleModel schedule)
        {
            throw new NotImplementedException();
        }
    }
}
