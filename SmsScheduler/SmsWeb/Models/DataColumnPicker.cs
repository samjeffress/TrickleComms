using System;
using System.Collections.Generic;

namespace SmsWeb.Models
{
    public class DataColumnPicker
    {
        public DataColumnPicker()
        {
            TemplateVariableColumns = new Dictionary<string, int?>();
        }

        public Guid TrickleId { get; set; }

        public bool FirstRowIsHeader { get; set; }

        public int? EmailColumn { get; set; }

        public int?  PhoneNumberColumn { get; set; }

        public int? CustomerNameColumn { get; set; }

        public int? CustomerIdColumn { get; set; }

        public Dictionary<string, int?> TemplateVariableColumns { get; set; }
    }
}