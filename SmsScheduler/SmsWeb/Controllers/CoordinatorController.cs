using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.SessionState;
using NServiceBus;
using SmsMessages.CommonData;
using SmsMessages.Coordinator;
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
            if (isValid && SecondaryValidation(coordinatedMessages))
            {
                var coordinatorId = Guid.NewGuid();

                if (coordinatedMessages.TimeSeparator.HasValue && !coordinatedMessages.SendAllBy.HasValue)
                {
                    var trickleSmsSpacedByTimePeriod = Mapper.MapToTrickleSpacedByPeriod(coordinatedMessages);
                    trickleSmsSpacedByTimePeriod.CoordinatorId = coordinatorId;
                    Bus.Send(trickleSmsSpacedByTimePeriod);
                }
                if (!coordinatedMessages.TimeSeparator.HasValue && coordinatedMessages.SendAllBy.HasValue)
                {
                    var trickleSmsOverTimePeriod = Mapper.MapToTrickleOverPeriod(coordinatedMessages);
                    trickleSmsOverTimePeriod.CoordinatorId = coordinatorId;
                    Bus.Send(trickleSmsOverTimePeriod);    
                }

                return RedirectToAction("Details", "Coordinator", new {coordinatorId = coordinatorId.ToString()});
            }
            return View("Create", coordinatedMessages);
        }

        private bool SecondaryValidation(CoordinatedSharedMessageModel coordinatedMessages)
        {
            if (coordinatedMessages.StartTime < DateTime.Now)
                return false;
            if (coordinatedMessages.SendAllBy.HasValue && coordinatedMessages.SendAllBy.Value <= coordinatedMessages.StartTime)
                return false;
            if (coordinatedMessages.SendAllBy.HasValue && coordinatedMessages.TimeSeparator.HasValue)
                return false;
            if (!coordinatedMessages.SendAllBy.HasValue && !coordinatedMessages.TimeSeparator.HasValue)
                return false;
            return true;
        }

        public ActionResult Details(string coordinatorid)
        {
            using (var session = RavenDocStore.GetStore().OpenSession())
            {
                var coordinatorTrackingData = session.Load<CoordinatorTrackingData>(coordinatorid);
                if (coordinatorTrackingData == null)
                    throw new NotImplementedException();
                    //return View("DetailsNotCreated", scheduleId);
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
            
            Bus.Send(new ResumeTrickledMessages { CoordinatorId = Guid.Parse(coordinatorid), ResumeTime = timeToResume});
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
                    model.Numbers.Select(n => new SmsData(n, model.Message)).
                    ToList(),
                StartTime = model.StartTime,
                MetaData = new SmsMetaData { Tags = model.Tags, Topic = model.Topic }
            };
        }

        public TrickleSmsWithDefinedTimeBetweenEachMessage MapToTrickleSpacedByPeriod(CoordinatedSharedMessageModel model)
        {
            return new TrickleSmsWithDefinedTimeBetweenEachMessage
            {
                Messages =
                    model.Numbers.Select(n => new SmsData(n, model.Message)).
                    ToList(),
                StartTime = model.StartTime,
                TimeSpacing = model.TimeSeparator.Value,
                MetaData = new SmsMetaData { Tags = model.Tags, Topic = model.Topic }
            };
        }
    }
}
