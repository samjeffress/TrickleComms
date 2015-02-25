using System.Collections.Generic;

namespace ConfigurationModels
{
	public class EmailTemplate
	{
		public string TemplateName { get; set; }
		public string TemplateContent { get; set; }
		public List<TemplateVariable> TemplateVariables { get; set; }
	}

	public class TemplateVariable
	{
		public string TemplateName { get; set; }
		public bool Mandatory { get; set; }
	}
}