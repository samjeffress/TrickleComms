using System;
using System.Collections.Generic;
using System.Linq;

namespace ConfigurationModels
{
	public class CommunicationTemplate
	{
	    public CommunicationTemplate()
	    {
	        TemplateVariables = new List<TemplateVariable>();
	    }

		public string TemplateName { get; set; }
		public string EmailContent { get; set; }
		public string SmsContent { get; set; }
		public List<TemplateVariable> TemplateVariables { get; set; }

		public bool ValidEmailTemplate()
		{
			// check variables are correct mostly
			return true;
		}

	    public void ExtractVariables()
	    {
	        if (!string.IsNullOrWhiteSpace(EmailContent))
	        {
	            var startIndexes = EmailContent.IndexesOf("{");
	            var endIndexes = EmailContent.IndexesOf("}");

	            GetTemplateVariableNames(startIndexes, endIndexes, EmailContent);
	        }

	        if (!string.IsNullOrWhiteSpace(SmsContent))
	        {
                var startIndexes = SmsContent.IndexesOf("{");
                var endIndexes = SmsContent.IndexesOf("}");

                GetTemplateVariableNames(startIndexes, endIndexes, SmsContent);
	        }
	    }

	    private void GetTemplateVariableNames(List<int> startIndexes, List<int> endIndexes, string content)
	    {
	        if (startIndexes.Count != endIndexes.Count)
	        {
	            throw new Exception("Odd number of opening and closing brackets in template");
	        }

	        for (int i = 0; i < startIndexes.Count; i++)
	        {
	            if (startIndexes[i] > endIndexes[i])
	            {
	                throw new Exception("Brackets must be open and closed before creating new ones");
	            }
	            if (i != 0)
	            {
	                if (startIndexes[i] < endIndexes[i - 1])
	                    throw new Exception("Brackets must be open and closed before creating new ones");
	            }
	        }

	        for (var i = 0; i < startIndexes.Count; i++)
	        {
	            int variableLength = endIndexes[i] - (startIndexes[i] + 1);
	            var variableName = content.Substring(startIndexes[i] + 1, variableLength);
                if (!TemplateVariables.Any(t => t.VariableName.Equals(variableName, StringComparison.CurrentCultureIgnoreCase)))
                {
                    TemplateVariables.Add(new TemplateVariable
                    {
                        VariableName = variableName
                    });
                }
	        }
	    }
	}

    public static class StringExtension
    {
        public static List<int> IndexesOf(this string content, string instance)
        {
            var indexes = new List<int>();
            for (var index = 0; ; index += instance.Length)
            {
                index = content.IndexOf(instance, index, StringComparison.Ordinal);
                if (index == -1)
                    break;
                indexes.Add(index);
            }
            return indexes;
        }
    }

	public class TemplateVariable
	{
		public string VariableName { get; set; }
		public bool Mandatory { get; set; }
	}
}