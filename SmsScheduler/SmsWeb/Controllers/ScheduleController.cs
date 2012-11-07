using System;
using System.Web.Mvc;
using NServiceBus;
using SmsMessages.CommonData;
using SmsMessages.Scheduling;
using SmsTracking;
using SmsWeb.Models;

namespace SmsWeb.Controllers
{
    public class ScheduleController : Controller
    {
        public IBus Bus { get; set; }

        public IRavenDocStore RavenDocStore { get; set; }

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
                    SmsData = new SmsData(schedule.Number, schedule.MessageBody),
                    ScheduleMessageId = Guid.NewGuid()
                };
                Bus.Send(scheduleMessage);
                return RedirectToAction("Details", "Schedule", new { scheduleId = scheduleMessage.ScheduleMessageId.ToString() });
            }
            return View("Create", schedule);
        }

        public ActionResult Details(string scheduleId)
        {
            using (var session = RavenDocStore.GetStore().OpenSession())
            {
                var scheduleTrackingData = session.Load<ScheduleTrackingData>(scheduleId);
                if (scheduleTrackingData == null)
                    return View("DetailsNotCreated", scheduleId);
                return View("Details", scheduleTrackingData);
            }
        }
    }
}
