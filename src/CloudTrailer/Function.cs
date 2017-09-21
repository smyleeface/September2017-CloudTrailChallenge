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
using Amazon.S3.Model;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using CloudTrailer.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace CloudTrailer
{
    public class S3Data {
            public string s3Bucket { get; set; }
            public IList<string> s3ObjectKey { get; set; }
    }


    public class Function
    {
        public static byte[] ReadStream(Stream responseStream)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = responseStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }
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
            var snsMessage = JsonConvert.DeserializeObject<S3Data>(evnt.Records[0].Sns.Message);
            String s3Bucket = snsMessage.s3Bucket;
            String s3ObjectKey = snsMessage.s3ObjectKey[0];

            context.Logger.LogLine(s3Bucket);
            context.Logger.LogLine(s3ObjectKey);
            GetObjectRequest request = new GetObjectRequest {
                BucketName = s3Bucket,
                Key = s3ObjectKey
            };
            var response = await S3Client.GetObjectAsync(request);

            using (Stream reader = response.ResponseStream) {
                var bytes = ReadStream(reader);
                await ExtractCloudTrailRecordsAsync(context.Logger, bytes);
            }

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