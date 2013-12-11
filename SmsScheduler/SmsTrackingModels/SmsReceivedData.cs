using System;
using SmsMessages.CommonData;

namespace SmsTrackingModels
{
    public class SmsReceivedData
    {
        public SmsData SmsData { get; set; }

        public SmsConfirmationData SmsConfirmationData { get; set; }

        public Guid SmsId { get; set; }

        public bool Acknowledge { get; set; }
    }
}