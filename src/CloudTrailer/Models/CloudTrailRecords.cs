namespace CloudTrailer.Models
{
    internal class CloudTrailRecords
    {
        public CloudTrailEvent[] Records { get; set; } = new CloudTrailEvent[0];
    }
}