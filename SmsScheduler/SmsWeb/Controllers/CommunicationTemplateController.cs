using System;
using System.Linq;
using System.Web.Mvc;
using ConfigurationModels;
using SmsWeb.Models;

namespace SmsWeb.Controllers
{
    public class CommunicationTemplateController : Controller
    {
		public IRavenDocStore Raven { get; set; }
		public ICurrentUser CurrentUser { get; set; }

        public ActionResult Index()
        {
			using (var session = Raven.GetStore().OpenSession("Configuration"))
			{
				// TODO: Paging ?? 
				var templates = session.Query<CommunicationTemplate>().ToList();
				return View (templates);
			}
        }

		public ActionResult Details(string templateName)
        {
			using(var session = Raven.GetStore().OpenSession("Configuration"))
			{
				var existingTemplate = session.Load<CommunicationTemplate>(templateName);
				if (existingTemplate == null)
				{
					throw new Exception("should have a document here...");
				}
				return View (existingTemplate);
			}
        }

        public ActionResult Create()
        {
            return View ();
        } 

        [HttpPost]
        public ActionResult Create(TemplateModel model)
        {
            try {
				using(var session = Raven.GetStore().OpenSession("Configuration")){
					var existingTemplate = session.Load<CommunicationTemplate>(model.TemplateName);
					if (existingTemplate != null){
						throw new Exception("shouldn't exist");
					}
                    
				    var communicationTemplate = new CommunicationTemplate()
				    {
				        EmailContent = model.EmailContent,
				        SmsContent = model.SmsContent,
				        TemplateName = model.TemplateName,
                        // TODO : Exstract template variables....
				        TemplateVariables = null
				    };
				    communicationTemplate.ExtractVariables();
					session.Store(communicationTemplate, communicationTemplate.TemplateName);
					session.SaveChanges();
				}
                return RedirectToAction ("Index");
            } catch {
                return View ();
            }
        }
        
        public ActionResult Edit(string templateName)
        {
			using(var session = Raven.GetStore().OpenSession("Configuration"))
			{
				var existingTemplate = session.Load<CommunicationTemplate>(templateName);
				if (existingTemplate == null)
				{
					throw new Exception("should have a document here...");
				}
				return View(existingTemplate);
			}
        }

        [HttpPost]
        public ActionResult Edit(string templateName, CommunicationTemplate model)
        {
            try {
				using(var session = Raven.GetStore().OpenSession("Configuration"))
				{
					var existingTemplate = session.Load<CommunicationTemplate>(templateName);
					if (existingTemplate == null)
					{
						throw new Exception("should have a document here...");
					}
					existingTemplate = model;
					session.SaveChanges();
				}
                return RedirectToAction ("Index");
            } catch {
                return View ();
            }
        }

        public ActionResult Delete(string templateName)
        {
			using(var session = Raven.GetStore().OpenSession("Configuration"))
			{
				var existingTemplate = session.Load<CommunicationTemplate>(templateName);
				if (existingTemplate == null)
				{
					throw new Exception("should have a document here...");
				}
				return View (existingTemplate);
			}
        }

        [HttpPost]
        public ActionResult Delete(CommunicationTemplate template)
        {
            try {
				using(var session = Raven.GetStore().OpenSession("Configuration"))
				{
                    var existingTemplate = session.Load<CommunicationTemplate>(template.TemplateName);
					if (existingTemplate == null)
					{
						throw new Exception("should have a document here...");
					}
					session.Delete(existingTemplate);
					session.SaveChanges();
				}
                return RedirectToAction ("Index");
            } catch {
                return View ();
            }
        }
    }
}