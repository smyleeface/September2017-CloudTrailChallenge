namespace CloudTrailer.Models
{
    internal class CloudTrailMessage
    {
        public string S3Bucket { get; set; }
        public string[] S3ObjectKey { get; set; }
    }
}