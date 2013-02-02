using System;
using System.Linq;
using System.Web.Mvc;
using ConfigurationModels;
using NServiceBus;
using SmsMessages.CommonData;
using SmsMessages.Scheduling.Commands;
using SmsTrackingModels;
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
            if (schedule.ScheduledTime < DateTime.Now)
                ModelState.AddModelError("ScheduledTime", "Schedule time must be in the future");
            var isValid = TryValidateModel(schedule);
            if (isValid)
            {
                if (schedule.MessageBody.Length > 160)
                    schedule.MessageBody = schedule.MessageBody.Substring(0, 160);
                CountryCodeReplacement countryCodeReplacement;
                using (var session = RavenDocStore.GetStore().OpenSession("Configuration"))
                {
                    countryCodeReplacement = session.Load<CountryCodeReplacement>("CountryCodeConfig");
                }
                var cleanInternationalNumber = countryCodeReplacement != null ? countryCodeReplacement.CleanAndInternationaliseNumber(schedule.Number) : schedule.Number.Trim();
                var scheduleMessage = new ScheduleSmsForSendingLater
                {
                    SendMessageAtUtc = schedule.ScheduledTime.ToUniversalTime(),
                    SmsData = new SmsData(cleanInternationalNumber, schedule.MessageBody),
                    ScheduleMessageId = Guid.NewGuid(),
                    SmsMetaData = new SmsMetaData
                    {
                        Tags = string.IsNullOrWhiteSpace(schedule.Tags) ? null : schedule.Tags.Split(',').ToList(), 
                        Topic = schedule.Topic
                    },
                    ConfirmationEmail = schedule.ConfirmationEmail
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
                    return View("DetailsNotCreated", (object)scheduleId);
                return View("Details", scheduleTrackingData);
            }
        }
    }
}
