using System;
using System.Web.Mvc;
using NServiceBus;
using SmsMessages.CommonData;
using SmsMessages.Scheduling;
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
            var isValid = TryValidateModel(schedule);
            if (isValid && schedule.ScheduledTime > DateTime.Now)
            {
                var scheduleMessage = new ScheduleSmsForSendingLater
                {
                    SendMessageAt = schedule.ScheduledTime,
                    SmsData = new SmsData(schedule.Number, schedule.MessageBody)
                };
                Bus.Send(scheduleMessage);
                return View("Details", schedule);
            }
            return View("Create", schedule);
        }
    }
}
