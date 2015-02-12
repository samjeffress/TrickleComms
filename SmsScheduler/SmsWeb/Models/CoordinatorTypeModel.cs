using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace SmsWeb.Models
{
    public abstract class CoordinatorTypeModel
    {
		[Obsolete("This refers to 'coordinator with defined time between messages' which is no longer available")]
        public int? TimeSeparatorSeconds { get; set; }

        public DateTime? SendAllBy { get; set; }

        public bool? SendAllAtOnce { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public string UserTimeZone { get; set; }

        public string Tags { get; set; }

        public string Topic { get; set; }

        public string ConfirmationEmail { get; set; }

        public List<Guid> CoordinatorsToExclude { get; set; }
        public List<string> GetTagList()
        {
            return string.IsNullOrWhiteSpace(Tags) ? null : Tags.Split(new[] { ',', ';', ':' }).ToList().Select(t => t.Trim()).ToList();
        }

        public List<string> GetEmailList()
        {
            return string.IsNullOrWhiteSpace(ConfirmationEmail) ? null : ConfirmationEmail.Split(new[] { ',', ';', ':' }).ToList().Select(t => t.Trim()).ToList();
        }

        public bool IsMessageTypeValid()
        {
            try
            {
                GetMessageTypeFromModel();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public abstract Type GetMessageTypeFromModel();
    }
}