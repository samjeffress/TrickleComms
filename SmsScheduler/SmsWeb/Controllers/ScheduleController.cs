using System;
using System.Linq;
using System.Web.Mvc;
using NServiceBus;
using SmsMessages.CommonData;
using SmsMessages.Scheduling.Commands;
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
        public ActionResult Create(FormCollection collection)
        {
            var schedule = ParseFormData(collection);
            if (schedule.ScheduledTime < DateTime.Now)
                ModelState.AddModelError("ScheduledTime", "Schedule time must be in the future");
            var isValid = TryValidateModel(schedule);
            if (isValid)
            {
                var scheduleMessage = new ScheduleSmsForSendingLater
                {
                    SendMessageAtUtc = schedule.ScheduledTime.ToUniversalTime(),
                    SmsData = new SmsData(schedule.Number, schedule.MessageBody),
                    ScheduleMessageId = Guid.NewGuid(),
                    SmsMetaData = new SmsMetaData { Tags = schedule.Tags, Topic = schedule.Topic },
                    ConfirmationEmail = schedule.ConfirmationEmail
                };
                Bus.Send(scheduleMessage);
                return RedirectToAction("Details", "Schedule", new { scheduleId = scheduleMessage.ScheduleMessageId.ToString() });
            }
            ViewBag.tags = collection["tag"];
            return View("Create", schedule);
        }

        private ScheduleModel ParseFormData(FormCollection formCollection)
        {
            var scheduleModel = new ScheduleModel();
            scheduleModel.MessageBody = formCollection["MessageBody"];
            scheduleModel.Number = formCollection["Number"];
            if (hasValue(formCollection, "ScheduledTime"))
                scheduleModel.ScheduledTime = DateTime.Parse(formCollection["ScheduledTime"]);
            if (hasValue(formCollection, "tag"))
                scheduleModel.Tags = formCollection["tag"].Split(',').ToList().Select(t => t.Trim()).ToList();
            scheduleModel.Topic = formCollection["Topic"];
            scheduleModel.ConfirmationEmail = formCollection["ConfirmationEmail"];
            return scheduleModel;
        }

        private bool hasValue(FormCollection formCollection, string key)
        {
            if (formCollection[key] != null && !string.IsNullOrWhiteSpace(formCollection[key]))
                return true;
            return false;
        }

        public ActionResult Details(string scheduleId)
        {
            using (var session = RavenDocStore.GetStore().OpenSession())
            {
                var scheduleTrackingData = session.Load<ScheduleTrackingData>(scheduleId);
                if (scheduleTrackingData == null)
                    return View("DetailsNotCreated", (object)scheduleId);
                return View("Details", scheduleTrackingData);
            }
        }
    }
}
