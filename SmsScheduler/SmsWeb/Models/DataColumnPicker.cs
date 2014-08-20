using System;

namespace SmsWeb.Models
{
    public class DataColumnPicker
    {
        public Guid TrickleId { get; set; }

        public bool FirstRowIsHeader { get; set; }

        public int? EmailColumn { get; set; }

        public int?  PhoneNumberColumn { get; set; }

        public int? CustomerNameColumn { get; set; }

        public int? CustomerIdColumn { get; set; }
    }
}