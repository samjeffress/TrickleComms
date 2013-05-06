using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using ConfigurationModels;
using NServiceBus;
using Raven.Client;
using SmsMessages.Coordinator.Commands;
using SmsTrackingModels;
using SmsWeb.Models;

namespace SmsWeb.Controllers
{
    public class CoordinatorController : Controller
    {
        public IBus Bus { get; set; }

        public IRavenDocStore RavenDocStore { get; set; }

        public ICoordinatorModelToMessageMapping Mapper { get; set; }

        public IDateTimeUtcFromOlsenMapping DateTimeOlsenMapping { get; set; }

        public ActionResult Index()
        {
            using (var session = RavenDocStore.GetStore().OpenSession())
            {
                var coordinatorTrackingData = session.Query<CoordinatorTrackingData>()
                    .Where(c => c.CurrentStatus != CoordinatorStatusTracking.Completed)
                    .ToList();
                return View("Index", coordinatorTrackingData);
            }
        }

        public ActionResult Create()
        {
            List<SelectListItem> selectListItems;
            using (var session = RavenDocStore.GetStore().OpenSession("SmsTracking"))
            {
                var thing = session.Query<CoordinatorTrackingData>().OrderByDescending(c => c.CreationDateUtc).Take(10).ToList();
                selectListItems = thing.Select(c => new SelectListItem { Selected = false, Text = CoordinatorToExcludeText(c), Value = c.CoordinatorId.ToString() }).ToList();
            }
            ViewData.Add("CoordinatorExcludeList", selectListItems);
            return View("Create");
        }

        [HttpPost]
        public ActionResult Create(CoordinatedSharedMessageModel coordinatedMessages)
        {
            var isValid = TryValidateModel(coordinatedMessages);
            SecondaryValidation(coordinatedMessages);
            if (isValid && ModelState.IsValid)
            {
                CountryCodeReplacement countryCodeReplacement;
                using (var session = RavenDocStore.GetStore().OpenSession("Configuration"))
                {
                    countryCodeReplacement = session.Load<CountryCodeReplacement>("CountryCodeConfig");
                }

                var excludeList = new List<string>();
                using (var session = RavenDocStore.GetStore().OpenSession("SmsTracking"))
                {
                    foreach (var previousCoordinatorId in coordinatedMessages.CoordinatorsToExclude)
                    {
                        var previousCoordinator = session.Load<CoordinatorTrackingData>(previousCoordinatorId.ToString());
                        excludeList.AddRange(previousCoordinator.MessageStatuses.Select(p => p.Number).ToList());
                    }
                }

                var cleanExcludeList = excludeList.Distinct().ToList();

                var coordinatorId = Guid.NewGuid();
                if (coordinatedMessages.Message.Length > 160)
                    coordinatedMessages.Message = coordinatedMessages.Message.Substring(0, 160);
                var messageTypeRequired = coordinatedMessages.GetMessageTypeFromModel();
                if (messageTypeRequired == typeof(TrickleSmsWithDefinedTimeBetweenEachMessage))
                {
                    var trickleSmsSpacedByTimePeriod = Mapper.MapToTrickleSpacedByPeriod(coordinatedMessages, countryCodeReplacement, cleanExcludeList);
                    trickleSmsSpacedByTimePeriod.CoordinatorId = coordinatorId;
                    Bus.Send(trickleSmsSpacedByTimePeriod);
                }
                else if (messageTypeRequired == typeof(TrickleSmsOverCalculatedIntervalsBetweenSetDates))
                {
                    var trickleSmsOverTimePeriod = Mapper.MapToTrickleOverPeriod(coordinatedMessages, countryCodeReplacement, cleanExcludeList);
                    trickleSmsOverTimePeriod.CoordinatorId = coordinatorId;
                    Bus.Send(trickleSmsOverTimePeriod);    
                }
                else if (messageTypeRequired == typeof(SendAllMessagesAtOnce))
                {
                    var sendAllAtOnce = Mapper.MapToSendAllAtOnce(coordinatedMessages, countryCodeReplacement, cleanExcludeList);
                    sendAllAtOnce.CoordinatorId = coordinatorId;
                    Bus.Send(sendAllAtOnce);    
                }

                return RedirectToAction("Details", "Coordinator", new {coordinatorId = coordinatorId.ToString()});
            }

            var selectListItems = new List<SelectListItem>();
            
            using (var session = RavenDocStore.GetStore().OpenSession("SmsTracking"))
            {
                var last10Coordinators = session.Query<CoordinatorTrackingData>().OrderByDescending(c => c.CreationDateUtc).Take(10).ToList();
                selectListItems = last10Coordinators.Select(c => new SelectListItem { Selected = false, Text = CoordinatorToExcludeText(c), Value = c.CoordinatorId.ToString() }).ToList();
                foreach (var previousCoordinatorId in coordinatedMessages.CoordinatorsToExclude)
                {
                    if (last10Coordinators.Any(c => c.CoordinatorId == previousCoordinatorId))
                    {
                        selectListItems.First(s => Guid.Parse(s.Value) == previousCoordinatorId).Selected = true;
                    }
                    else
                    {
                        var coordinatorTrackingData = session.Load<CoordinatorTrackingData>(previousCoordinatorId.ToString());
                        selectListItems.Add(new SelectListItem { Selected = true, Text = coordinatorTrackingData.MetaData.Topic, Value = coordinatorTrackingData.CoordinatorId.ToString() });
                    }
                }
            }
            ViewData.Add("CoordinatorExcludeList", selectListItems);
            return View("Create", coordinatedMessages);
        }

        private string CoordinatorToExcludeText(CoordinatorTrackingData coordinatorTrackingData)
        {
            return string.Format(
                "'{0}', {1} Sent, Started {2}",
                coordinatorTrackingData.MetaData.Topic,
                coordinatorTrackingData.MessageStatuses.Count(c => c.Status == MessageStatusTracking.CompletedSuccess).ToString(),
                coordinatorTrackingData.CreationDateUtc.ToLocalTime().ToShortDateString());
        }

        public ActionResult History(int page = 0, int resultsPerPage = 20)
        {
            using (var session = RavenDocStore.GetStore().OpenSession())
            {
                RavenQueryStatistics stats;
                var pagedResults = session.Query<CoordinatorTrackingData>()
                    .Statistics(out stats)
                    .Skip(page * resultsPerPage)
                    .Take(resultsPerPage)
                    .OrderByDescending(c => c.CreationDateUtc)
                    .ToList()
                    .Select(c => new CoordinatorOverview
                    {
                        CurrentStatus = c.CurrentStatus,
                        MessageCount = c.MessageStatuses.Count,
                        CoordinatorId = c.CoordinatorId,
                        CreationDate = c.CreationDateUtc,
                        CompletionDate = c.CompletionDateUtc,
                        Tags = c.MetaData != null && c.MetaData.Tags != null ? c.MetaData.Tags : new List<string>(),
                        Topic = c.MetaData != null ? c.MetaData.Topic : string.Empty
                    })
                    .ToList();
                var totalResults = stats.TotalResults;
                var coordinatorPagedResults = new CoordinatorPagedResults
                {
                    CoordinatorOverviews = pagedResults, 
                    Page = page, 
                    ResultsPerPage = resultsPerPage, 
                    TotalResults = totalResults, 
                    TotalPages = (int) Math.Ceiling((double) totalResults/(double) resultsPerPage)
                };
                return View("CoordinatorPagedOverview", coordinatorPagedResults);
            }
        }

        private void SecondaryValidation(CoordinatedSharedMessageModel coordinatedMessages)
        {
            if (string.IsNullOrWhiteSpace(coordinatedMessages.Topic))
                ModelState.AddModelError("Topic", "Topic must be set");
            if (coordinatedMessages.Numbers == null || coordinatedMessages.Numbers.Split(',').Length == 0)
                ModelState.AddModelError("numberList", "Please include the numbers you want to send to.");
            if (coordinatedMessages.StartTime < DateTime.Now.AddMinutes(-5))
                ModelState.AddModelError("StartTime", "Start Time must be in the future");
            if (coordinatedMessages.SendAllBy.HasValue && coordinatedMessages.SendAllBy.Value <= coordinatedMessages.StartTime)
                ModelState.AddModelError("SendAllBy", "SendAllBy time must be after StartTime");
            if (!coordinatedMessages.IsMessageTypeValid())
                ModelState.AddModelError("SendAllBy","Message must contain either Time Separator OR DateTime to send all messages by.");
        }

        public ActionResult Details(string coordinatorid)
        {
            using (var session = RavenDocStore.GetStore().OpenSession())
            {
                var coordinatorTrackingData = session.Load<CoordinatorTrackingData>(coordinatorid);
                if (coordinatorTrackingData == null)
                {
                    return View("DetailsNotCreated", model: coordinatorid);
                }

                if (HttpContext.Session != null && HttpContext.Session["CoordinatorState"] != null && HttpContext.Session["CoordinatorState"] is CoordinatorStatusTracking)
                    coordinatorTrackingData.CurrentStatus = (CoordinatorStatusTracking)HttpContext.Session["CoordinatorState"];
                return View("Details", coordinatorTrackingData);
            }
        }

        [HttpPost]
        public ActionResult Pause(FormCollection collection)
        {
            var coordinatorid = collection["CoordinatorId"];
            Bus.Send(new PauseTrickledMessagesIndefinitely { CoordinatorId = Guid.Parse(coordinatorid), MessageRequestTimeUtc = DateTime.UtcNow });
            HttpContext.Session.Add("CoordinatorState", CoordinatorStatusTracking.Paused);
            return RedirectToAction("Details", new { coordinatorid });
        }

        [HttpPost]
        public ActionResult Resume(FormCollection collection)
        {
            var coordinatorid = collection["CoordinatorId"];
            var timeToResume = DateTime.Now;
            var timeToResumeParsed = DateTime.TryParse(collection["timeToResume"], out timeToResume);
            var userTimeZone = collection["UserTimeZone"];
            var finishTime = DateTime.Now;
            var finishTimeParsed = DateTime.TryParse(collection["finishTime"], out finishTime);

            // validate
            if (!timeToResumeParsed)
                ModelState.AddModelError("timeToResume", "Time to resume must be set");
            if (timeToResume < DateTime.Now.AddMinutes(-5))
                ModelState.AddModelError("timeToResume", "Time to resume must be in the future");
            if (finishTimeParsed && finishTime <= timeToResume)
                ModelState.AddModelError("finishTime", "Finish time must be after time to resume");

            if (!ModelState.IsValid)
            {
                using (var session = RavenDocStore.GetStore().OpenSession())
                {
                    var coordinatorTrackingData = session.Load<CoordinatorTrackingData>(coordinatorid);
                    if (coordinatorTrackingData == null)
                    {
                        return View("DetailsNotCreated", model: coordinatorid);
                    }

                    if (HttpContext.Session != null && HttpContext.Session["CoordinatorState"] != null && HttpContext.Session["CoordinatorState"] is CoordinatorStatusTracking)
                        coordinatorTrackingData.CurrentStatus = (CoordinatorStatusTracking)HttpContext.Session["CoordinatorState"];
                    ViewData.Add("timeToResume", collection["timeToResume"]);
                    ViewData.Add("finishTime", collection["finishTime"]);
                    return View("Details", coordinatorTrackingData);
                }              
            }

            var dateTimeToResumeUtc = DateTimeOlsenMapping.DateTimeWithOlsenZoneToUtc(timeToResume, userTimeZone);
            if (finishTimeParsed)
            {
                var dateTimeToFinishUtc = DateTimeOlsenMapping.DateTimeWithOlsenZoneToUtc(finishTime, userTimeZone);
                Bus.Send(new RescheduleTrickledMessages { CoordinatorId = Guid.Parse(coordinatorid), ResumeTimeUtc = dateTimeToResumeUtc, FinishTimeUtc = dateTimeToFinishUtc, MessageRequestTimeUtc = DateTime.UtcNow });
            }
            else
            {
                Bus.Send(new ResumeTrickledMessages { CoordinatorId = Guid.Parse(coordinatorid), ResumeTimeUtc = dateTimeToResumeUtc, MessageRequestTimeUtc = DateTime.UtcNow });    
            }
            
            HttpContext.Session.Add("CoordinatorState", CoordinatorStatusTracking.Started);
            return RedirectToAction("Details", new { coordinatorid });
        }
    }
}
