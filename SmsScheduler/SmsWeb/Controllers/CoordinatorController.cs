using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using ConfigurationModels;
using NServiceBus;
using Raven.Client;
using SmsMessages.CommonData;
using SmsMessages.Coordinator.Commands;
using SmsTrackingModels;
using SmsTrackingModels.RavenIndexs;
using SmsWeb.Models;

namespace SmsWeb.Controllers
{
    public class CoordinatorController : Controller
    {
        public ICurrentUser CurrentUser { get; set; }

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
                        excludeList.AddRange(previousCoordinator.GetListOfCoordinatedSchedules(RavenDocStore.GetStore()).Select(p => p.Number).ToList());
                    }
                }

                var cleanExcludeList = excludeList.Distinct().ToList();

                var coordinatorId = Guid.NewGuid();
                if (coordinatedMessages.Message.Length > 160)
                    coordinatedMessages.Message = coordinatedMessages.Message.Substring(0, 160);
                var messageTypeRequired = coordinatedMessages.GetMessageTypeFromModel();
				if (messageTypeRequired == typeof(TrickleSmsOverCalculatedIntervalsBetweenSetDates)) {
					var trickleSmsOverTimePeriod = Mapper.MapToTrickleOverPeriod (coordinatedMessages, countryCodeReplacement, cleanExcludeList, CurrentUser.Name ());
					trickleSmsOverTimePeriod.CoordinatorId = coordinatorId;
					Bus.Send (trickleSmsOverTimePeriod);    
				} else if (messageTypeRequired == typeof(SendAllMessagesAtOnce)) {
					var sendAllAtOnce = Mapper.MapToSendAllAtOnce (coordinatedMessages, countryCodeReplacement, cleanExcludeList, CurrentUser.Name ());
					sendAllAtOnce.CoordinatorId = coordinatorId;
					Bus.Send (sendAllAtOnce);    
				} else {
					throw new NotImplementedException ("This option has been removed");
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

        private string CoordinatorToExcludeText(CoordinatorTrackingData coordinatorTrackingData)
        {
            return string.Format(
                "'{0}', {1} Sent, Started {2}",
                coordinatorTrackingData.MetaData.Topic,
                coordinatorTrackingData.GetListOfCoordinatedSchedules(RavenDocStore.GetStore()).Count(c => c.Status == MessageStatusTracking.CompletedSuccess).ToString(),
                coordinatorTrackingData.CreationDateUtc.ToLocalTime().ToShortDateString());
        }

        public ActionResult History(int page = 0, int resultsPerPage = 20)
        {
            using (var session = RavenDocStore.GetStore().OpenSession())
            {
                RavenQueryStatistics stats;
                var pagedResults = session.Query<CoordinatorTrackingData, CoordinatorTrackingDataByDate>()
                    .Statistics(out stats)
                    .Skip(page * resultsPerPage)
                    .Take(resultsPerPage)
                    .OrderByDescending(c => c.CreationDateUtc)
                    .ToList()
                    .Select(c => new CoordinatorOverview
                    {
                        CurrentStatus = c.CurrentStatus,
                        MessageCount = c.GetCountOfSchedules(RavenDocStore.GetStore()), // TODO : Use of GetListOfCoordinatedSchedules can be done differently
                        CoordinatorId = c.CoordinatorId,
                        CreationDateUtc = c.CreationDateUtc,
                        CompletionDateUtc = c.CompletionDateUtc,
                        Tags = c.MetaData != null && c.MetaData.Tags != null ? c.MetaData.Tags : new List<string>(),
                        Topic = c.MetaData != null ? c.MetaData.Topic : string.Empty
                    })
                    .OrderByDescending(c => c.CreationDateUtc)
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
                var coordinatorSummary = session.Query<ScheduledMessagesStatusCountInCoordinatorIndex.ReduceResult, ScheduledMessagesStatusCountInCoordinatorIndex>()
                    .Where(s => s.CoordinatorId == coordinatorid)
                    .ToList();
                var coordinatorTrackingData = session.Load<CoordinatorTrackingData>(coordinatorid);
                if (coordinatorSummary.Count == 0 || coordinatorTrackingData == null)
                {
                    return View("DetailsNotCreated", model: coordinatorid);
                }

                DateTime? nextSmsDateUtc = session.Query<ScheduleTrackingData, ScheduleMessagesInCoordinatorIndex>()
                    .Where(s => s.CoordinatorId == Guid.Parse(coordinatorid) && s.MessageStatus == MessageStatus.Scheduled)
                    .OrderBy(s => s.ScheduleTimeUtc)
                    .Select(s => s.ScheduleTimeUtc)
                    .FirstOrDefault();
                if (nextSmsDateUtc == DateTime.MinValue)
                    nextSmsDateUtc = null;

                DateTime? finalSmsDateUtc = session.Query<ScheduleTrackingData, ScheduleMessagesInCoordinatorIndex>()
                    .Where(s => s.CoordinatorId== Guid.Parse(coordinatorid) && s.MessageStatus == MessageStatus.Scheduled)
                    .OrderByDescending(s => s.ScheduleTimeUtc)
                    .Select(s => s.ScheduleTimeUtc)
                    .FirstOrDefault();
                if (finalSmsDateUtc == DateTime.MinValue)
                    finalSmsDateUtc = null;

                var overview = new CoordinatorOverview(coordinatorTrackingData, coordinatorSummary);
                overview.NextScheduledMessageDate = nextSmsDateUtc;
                overview.FinalScheduledMessageDate = finalSmsDateUtc;
                if (HttpContext.Session != null && HttpContext.Session["CoordinatorState_" + coordinatorid] != null && HttpContext.Session["CoordinatorState_" + coordinatorid] is CoordinatorStatusTracking)
                    overview.CurrentStatus = (CoordinatorStatusTracking)HttpContext.Session["CoordinatorState_" + coordinatorid];
                return View("Details3", overview);
            }
        }

        [HttpPost]
        public ActionResult Pause(FormCollection collection)
        {
            var coordinatorid = collection["CoordinatorId"];
            Bus.Send(new PauseTrickledMessagesIndefinitely { CoordinatorId = Guid.Parse(coordinatorid), MessageRequestTimeUtc = DateTime.UtcNow });
            HttpContext.Session.Add("CoordinatorState_" + coordinatorid, CoordinatorStatusTracking.Paused);
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
                    var coordinatorSummary = session.Query<ScheduledMessagesStatusCountInCoordinatorIndex.ReduceResult, ScheduledMessagesStatusCountInCoordinatorIndex>()
                        .Where(s => s.CoordinatorId == coordinatorid)
                        .ToList();
                    var coordinatorTrackingData = session.Load<CoordinatorTrackingData>(coordinatorid);
                    if (coordinatorTrackingData == null)
                    {
                        return View("DetailsNotCreated", model: coordinatorid);
                    }

                    if (HttpContext.Session != null && HttpContext.Session["CoordinatorState_" + coordinatorid] != null && HttpContext.Session["CoordinatorState_" + coordinatorid] is CoordinatorStatusTracking)
                        coordinatorTrackingData.CurrentStatus = (CoordinatorStatusTracking)HttpContext.Session["CoordinatorState_" + coordinatorid];
                    ViewData.Add("timeToResume", collection["timeToResume"]);
                    ViewData.Add("finishTime", collection["finishTime"]);
                    var overview = new CoordinatorOverview(coordinatorTrackingData, coordinatorSummary);
                    return View("Details3", overview);
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

            HttpContext.Session.Add("CoordinatorState_" + coordinatorid, CoordinatorStatusTracking.Started);
            return RedirectToAction("Details", new { coordinatorid });
        }

        public PartialViewResult ScheduleFailedDetails(Guid coordinatorId, int page = 0, int pageSize = 20)
        {
            using (var session = RavenDocStore.GetStore().OpenSession())
            {
                RavenQueryStatistics stats;
                var pagedResults = session.Query<ScheduleTrackingData>("ScheduleMessagesInCoordinatorIndex")
                    .Statistics(out stats)
                    .Skip(page*pageSize)
                    .Take(pageSize)
                    .Where(s => s.CoordinatorId == coordinatorId)
                    .Where(s => s.MessageStatus == MessageStatus.Failed)
                    .Select(s => new ScheduleFailedModel
                                     {
                                         ScheduleId = s.ScheduleId,
                                         Number = s.SmsData.Mobile,
                                         ErrorMessage = s.SmsFailureData.Message,
                                         ErrorMoreInfo = s.SmsFailureData.MoreInfo,
                                         ErrorCode = s.SmsFailureData.Code
                                     })
                    .ToList();
               
                var pages = (int)Math.Ceiling((double)stats.TotalResults / (double)pageSize);
                var pagedResult = new PagedResult<ScheduleFailedModel> {CurrentPage = page, TotalPages = pages, ResultsPerPage = pageSize, ResultsList = pagedResults, CoordinatorId = coordinatorId};
                return PartialView("CoordinatorSchedulesFailed", pagedResult);
            }
        }
    }
}
