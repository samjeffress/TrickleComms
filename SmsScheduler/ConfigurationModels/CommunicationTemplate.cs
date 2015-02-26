using System.Collections.Generic;

namespace ConfigurationModels
{
	public class CommunicationTemplate
	{
		public string TemplateName { get; set; }
		public string EmailContent { get; set; }
		public string SmsContent { get; set; }
		public List<TemplateVariable> TemplateVariables { get; set; }

		public bool ValidEmailTemplate()
		{
			// check variables are correct mostly
			return true;
		}
	}

	public class TemplateVariable
	{
		public string TemplateName { get; set; }
		public bool Mandatory { get; set; }
	}
}