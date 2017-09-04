using System;
using System.Collections.Generic;

namespace CloudTrailer.Models
{
    public class CloudTrailEvent
    {
        public string EventVersion { get; set; }
        public Dictionary<string, object> UserIdentity { get; set; }
        public DateTime EventTime { get; set; }
        public string EventSource { get; set; }
        public string EventName { get; set; }
        public string AwsRegion { get; set; }
        public string SourceIpAddress { get; set; }
        public string UserAgent { get; set; }
        public Dictionary<string, object> RequestParameters { get; set; }
        public Dictionary<string, object> ResponseElements { get; set; }
        public Guid RequestId { get; set; }
        public Guid EventId { get; set; }
        public string EventType { get; set; }
        public string RecipientAccountId { get; set; }
    }
}