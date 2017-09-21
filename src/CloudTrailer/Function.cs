using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using Amazon.S3;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using CloudTrailer.Models;
using Newtonsoft.Json;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace CloudTrailer
{
    public class Function
    {
        private static readonly byte[] GZipHeaderBytes = {0x1f, 0x8b};
//        private static readonly byte[] GZipHeaderBytes = {0x1f, 0x8b, 8, 0, 0, 0, 0, 0, 4, 0};

        private IAmazonS3 S3Client { get; }
        private IAmazonSimpleNotificationService SnsClient { get; }
        private IAmazonIdentityManagementService IamClient { get; }
        private static string AlertTopicArn => Environment.GetEnvironmentVariable("AlertTopicArn");

        /// <summary>
        /// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
        /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
        /// region the Lambda function is executed in.
        /// </summary>
        public Function()
        {
            S3Client = new AmazonS3Client();
            SnsClient = new AmazonSimpleNotificationServiceClient();
            IamClient = new AmazonIdentityManagementServiceClient();
        }

        public async Task FunctionHandler(SNSEvent evnt, ILambdaContext context)
        {
            // ### Level 1 - Create New Trail and Configure Lambda
            context.Logger.LogLine(JsonConvert.SerializeObject(evnt));

            // ### Level 2 - Retrieve Logs from S3

            // ### Level 3 - Filter for specific events and send alerts

            // ### Boss level - Take mitigating action
        }


        private async Task<CloudTrailRecords> ExtractCloudTrailRecordsAsync(ILambdaLogger logger, byte[] input)
        {
            var appearsGzipped = ResponseAppearsGzipped(input);
            logger.LogLine($"Input appears to be gzipped: {appearsGzipped}");
            if (appearsGzipped)
            {
                using (var contents = new MemoryStream())
                using (var gz = new GZipStream(new MemoryStream(input), CompressionMode.Decompress))
                {
                    await gz.CopyToAsync(contents);
                    input = contents.ToArray();
                }
            }

            var serializedRecords = Encoding.UTF8.GetString(input);
            logger.Log(serializedRecords);
            return JsonConvert.DeserializeObject<CloudTrailRecords>(serializedRecords);

            bool ResponseAppearsGzipped(byte[] bytes)
            {
                var header = new byte[GZipHeaderBytes.Length];
                Array.Copy(bytes, header, header.Length);
                return header.SequenceEqual(GZipHeaderBytes);
            }
        }
    }
}