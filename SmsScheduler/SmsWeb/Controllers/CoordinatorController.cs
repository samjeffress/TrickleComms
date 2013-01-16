using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using NServiceBus;
using Raven.Client;
using SmsMessages.CommonData;
using SmsMessages.Coordinator.Commands;
using SmsTracking;
using SmsWeb.Models;

namespace SmsWeb.Controllers
{
    public class CoordinatorController : Controller
    {
        public IBus Bus { get; set; }

        public IRavenDocStore RavenDocStore { get; set; }

        public ICoordinatorModelToMessageMapping Mapper { get; set; }

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
            return View("Create");
        }

        [HttpPost]
        public ActionResult Create(CoordinatedSharedMessageModel coordinatedMessages)
        {
            var isValid = TryValidateModel(coordinatedMessages);
            SecondaryValidation(coordinatedMessages);
            if (isValid && ModelState.IsValid)
            {
                var coordinatorId = Guid.NewGuid();
                if (coordinatedMessages.Message.Length > 160)
                    coordinatedMessages.Message = coordinatedMessages.Message.Substring(0, 160);
                if (coordinatedMessages.TimeSeparatorSeconds.HasValue && !coordinatedMessages.SendAllBy.HasValue)
                {
                    var trickleSmsSpacedByTimePeriod = Mapper.MapToTrickleSpacedByPeriod(coordinatedMessages);
                    trickleSmsSpacedByTimePeriod.CoordinatorId = coordinatorId;
                    Bus.Send(trickleSmsSpacedByTimePeriod);
                }
                if (!coordinatedMessages.TimeSeparatorSeconds.HasValue && coordinatedMessages.SendAllBy.HasValue)
                {
                    var trickleSmsOverTimePeriod = Mapper.MapToTrickleOverPeriod(coordinatedMessages);
                    trickleSmsOverTimePeriod.CoordinatorId = coordinatorId;
                    Bus.Send(trickleSmsOverTimePeriod);    
                }

                return RedirectToAction("Details", "Coordinator", new {coordinatorId = coordinatorId.ToString()});
            }
            return View("Create", coordinatedMessages);
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
            if (coordinatedMessages.Numbers == null || coordinatedMessages.Numbers.Split(',').Length == 0)
                ModelState.AddModelError("numberList", "Please include the numbers you want to send to.");
            if (coordinatedMessages.StartTime < DateTime.Now)
                ModelState.AddModelError("StartTime", "Start Time must be in the future");
            if (coordinatedMessages.SendAllBy.HasValue && coordinatedMessages.SendAllBy.Value <= coordinatedMessages.StartTime)
                ModelState.AddModelError("SendAllBy", "SendAllBy time must be after StartTime");
            if (coordinatedMessages.SendAllBy.HasValue && coordinatedMessages.TimeSeparatorSeconds.HasValue)
                ModelState.AddModelError("SendAllBy", "You must select either SendAllBy OR TimeSeparated - cannot pick both");
            if (!coordinatedMessages.SendAllBy.HasValue && !coordinatedMessages.TimeSeparatorSeconds.HasValue)
                ModelState.AddModelError("SendAllBy", "You must select either SendAllBy OR TimeSeparated - cannot have none");
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
            Bus.Send(new PauseTrickledMessagesIndefinitely { CoordinatorId = Guid.Parse(coordinatorid) });
            HttpContext.Session.Add("CoordinatorState", CoordinatorStatusTracking.Paused);
            return RedirectToAction("Details", new { coordinatorid });
        }

        [HttpPost]
        public ActionResult Resume(FormCollection collection)
        {
            var coordinatorid = collection["CoordinatorId"];
            var timeToResume = DateTime.Parse(collection["timeToResume"]);
            
            Bus.Send(new ResumeTrickledMessages { CoordinatorId = Guid.Parse(coordinatorid), ResumeTimeUtc = timeToResume.ToUniversalTime()});
            HttpContext.Session.Add("CoordinatorState", CoordinatorStatusTracking.Started);
            return RedirectToAction("Details", new { coordinatorid });
        }
    }

    public interface ICoordinatorModelToMessageMapping
    {
        TrickleSmsOverCalculatedIntervalsBetweenSetDates MapToTrickleOverPeriod(CoordinatedSharedMessageModel model);

        TrickleSmsWithDefinedTimeBetweenEachMessage MapToTrickleSpacedByPeriod(CoordinatedSharedMessageModel model);
    }

    public class CoordinatorModelToMessageMapping : ICoordinatorModelToMessageMapping
    {
        public TrickleSmsOverCalculatedIntervalsBetweenSetDates MapToTrickleOverPeriod(CoordinatedSharedMessageModel model)
        {
            return new TrickleSmsOverCalculatedIntervalsBetweenSetDates
            {
                Duration = model.SendAllBy.Value.Subtract(model.StartTime),
                Messages =
                    model.Numbers.Split(',').ToList().Select(n => new SmsData(n.Trim(), model.Message)).
                    ToList(),
                StartTimeUtc = model.StartTime.ToUniversalTime(),
                MetaData = new SmsMetaData
                {
                    Tags = string.IsNullOrWhiteSpace(model.Tags) ? null : model.Tags.Split(',').ToList().Select(t => t.Trim()).ToList(), 
                    Topic = model.Topic
                },
                ConfirmationEmail = model.ConfirmationEmail
            };
        }

        public TrickleSmsWithDefinedTimeBetweenEachMessage MapToTrickleSpacedByPeriod(CoordinatedSharedMessageModel model)
        {
            return new TrickleSmsWithDefinedTimeBetweenEachMessage
            {
                Messages =
                    model.Numbers.Split(',').ToList().Select(n => new SmsData(n.Trim(), model.Message)).
                    ToList(),
                StartTimeUtc = model.StartTime.ToUniversalTime(),
                TimeSpacing = TimeSpan.FromSeconds(model.TimeSeparatorSeconds.Value),
                MetaData = new SmsMetaData { Tags = model.Tags.Split(',').ToList().Select(t => t.Trim()).ToList(), Topic = model.Topic },
                ConfirmationEmail = model.ConfirmationEmail
            };
        }
    }
}
