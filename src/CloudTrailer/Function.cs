using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        }

        /// <summary>
        /// Constructs an instance with a preconfigured client. This can be used for testing the outside of the Lambda environment.
        /// </summary>
        /// <param name="s3Client">The client</param>
        public Function(IAmazonS3 s3Client, IAmazonSimpleNotificationService snsClient)
        {
            S3Client = s3Client;
            SnsClient = snsClient;
        }

        public async Task FunctionHandler(SNSEvent evnt, ILambdaContext context)
        {
            // ### Level 1 - Create New Trail and Configure Lambda
            context.Logger.LogLine(JsonConvert.SerializeObject(evnt));

            // ### Level 2 - Retrieve Logs from S3
            var loggedEvents = await RetrieveLogEvents(evnt, context);
            context.Logger.LogLine(JsonConvert.SerializeObject(loggedEvents));

            // ### Level 3 - Filter for specific events and send alerts
            var filteredEvents = FilterEvents(loggedEvents);
            await SendAlerts(filteredEvents);

            // ### Boss level - Take mitigating action
        }

        private Task<PublishResponse[]> SendAlerts(IEnumerable<CloudTrailEvent> filteredEvents)
        {
            var tasks = filteredEvents.Select(filteredEvent =>
                SnsClient.PublishAsync(AlertTopicArn, JsonConvert.SerializeObject(filteredEvent)));
            return Task.WhenAll(tasks);
        }

        private static IEnumerable<CloudTrailEvent> FilterEvents(IEnumerable<CloudTrailEvent> loggedEvents)
        {
            var interesting = new[] {"CreateUser"};
            return loggedEvents.Where(logged => interesting.Contains(logged.EventName))
                .ToList();
        }


        private async Task<List<CloudTrailEvent>> RetrieveLogEvents(SNSEvent evnt, ILambdaContext context)
        {
            var messages = evnt.Records.Select(r => r.Sns.Message)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(ConvertMessage)
                .SelectMany(message => ExtractRecords(context.Logger, message));

            var records = await Task.WhenAll(messages);
            return records.SelectMany(t => t.Records).ToList();
        }

        private IEnumerable<Task<CloudTrailRecords>> ExtractRecords(ILambdaLogger logger, CloudTrailMessage message)
        {
            return message.S3ObjectKey
                .Select(logKey => S3Client.GetObjectAsync(message.S3Bucket, logKey))
                .Select(async s3Request =>
                {
                    var response = await s3Request;
                    using (var inputStream = new MemoryStream())
                    {
                        await response.ResponseStream.CopyToAsync(inputStream);
                        return inputStream.ToArray();
                    }
                })
                .Select(async s3Response =>
                {
                    var input = await s3Response;
                    var appearsGzipped = ResponseAppearsGzipped(input);
                    logger.LogLine($"Input appears to be gzipped: {appearsGzipped}");
                    if (appearsGzipped)
                    {
                        input = await Decompress(input);
                    }

                    var serializedRecords = Encoding.UTF8.GetString(input);
                    logger.Log(serializedRecords);
                    return JsonConvert.DeserializeObject<CloudTrailRecords>(serializedRecords);
                });
        }

        private static async Task<byte[]> Decompress(byte[] input)
        {
            using (var contents = new MemoryStream())
            using (var gz = new GZipStream(new MemoryStream(input), CompressionMode.Decompress))
            {
                await gz.CopyToAsync(contents);
                return contents.ToArray();
            }
        }

        private static bool ResponseAppearsGzipped(byte[] input)
        {
            var header = new byte[GZipHeaderBytes.Length];
            Array.Copy(input, header, header.Length);
            return header.SequenceEqual(GZipHeaderBytes);
        }


        private static CloudTrailMessage ConvertMessage(string message)
        {
            return JsonConvert.DeserializeObject<CloudTrailMessage>(message);
        }
    }
}