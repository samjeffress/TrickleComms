using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using SmsTrackingModels;

namespace SmsWeb.Controllers
{
    public class TagController : Controller
    {
        public IRavenDocStore RavenDocStore { get; set; }

        public JsonResult Index()
        {
            using (var session = RavenDocStore.GetStore().OpenSession())
            {
                var reduceResults = session.Query<CoordinatorTagList.ReduceResult, CoordinatorTagList>()
                    .OrderByDescending(t => t.Count)
                    .Select(t => new { Label = t.Tag, Value= t.Tag })
                    .ToList();
                return Json(reduceResults, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult Search(string term)
        {
            using (var session = RavenDocStore.GetStore().OpenSession())
            {
                var reduceResults = session
                    .Query<CoordinatorTagList.ReduceResult, CoordinatorTagList>()
                    .Where(t => t.Tag.StartsWith(term, true, CultureInfo.CurrentCulture))
                    .ToList()
                    .Select(t => new { id = t.Tag, label = t.Tag, value = t.Tag })
                    .ToList();
                return Json(reduceResults, JsonRequestBehavior.AllowGet);
            }
        }
    }
}
