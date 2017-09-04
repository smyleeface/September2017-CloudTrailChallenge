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
using Newtonsoft.Json;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace CloudTrailer
{
    public class Function
    {
        private static readonly byte[] GZipHeaderBytes = {0x1f, 0x8b, 8, 0, 0, 0, 0, 0, 4, 0};

        private IAmazonS3 S3Client { get; }

        /// <summary>
        /// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
        /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
        /// region the Lambda function is executed in.
        /// </summary>
        public Function()
        {
            S3Client = new AmazonS3Client();
        }

        /// <summary>
        /// Constructs an instance with a preconfigured client. This can be used for testing the outside of the Lambda environment.
        /// </summary>
        /// <param name="s3Client">The client</param>
        public Function(IAmazonS3 s3Client)
        {
            S3Client = s3Client;
        }

        public async Task FunctionHandler(SNSEvent evnt, ILambdaContext context)
        {
            // ### Level 1 - Create New Trail and Configure Lambda
            context.Logger.LogLine(JsonConvert.SerializeObject(evnt));

            // ### Level 2 - Retrieve Logs from S3
            var logs = await RetrieveLogEvents(evnt, context);
            context.Logger.LogLine(JsonConvert.SerializeObject(logs));

            // ### Level 3 - Filter for specific events and send alerts

            // ### Boss level - Take mitigating action
        }

        private async Task<List<CloudTrailEvent>> RetrieveLogEvents(SNSEvent evnt, ILambdaContext context)
        {
            var messages = evnt.Records.Select(r => r.Sns.Message)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(message =>
                {
                    context.Logger.LogLine(message);
                    return JsonConvert.DeserializeObject<CloudTrailMessage>(message);
                })
                .SelectMany(message =>
                {
                    return message.S3ObjectKey.Select(async s =>
                    {
                        try
                        {
                            var response1 = await S3Client.GetObjectAsync(message.S3Bucket, s);
                            using (var inputStream1 = new MemoryStream())
                            {
                                await response1.ResponseStream.CopyToAsync(inputStream1);

                                var input1 = inputStream1.ToArray();
                                var header1 = new byte[GZipHeaderBytes.Length];
                                Array.Copy(input1, header1, header1.Length);

                                var gzipped1 = header1.SequenceEqual(GZipHeaderBytes);
                                context.Logger.LogLine($"Input appears to be gzipped: {gzipped1}");

                                if (gzipped1)
                                {
                                    response1.ResponseStream.Position = 0;

                                    using (var contents1 = new MemoryStream())
                                    using (var gz1 = new GZipStream(response1.ResponseStream, CompressionMode.Decompress))
                                    {
                                        await gz1.CopyToAsync(contents1);
                                        input1 = contents1.ToArray();
                                    }
                                }

                                context.Logger.Log($"{s}: read {input1.Length} bytes.");
                                var serializedRecords1 = Encoding.UTF8.GetString(input1);
                                context.Logger.Log(serializedRecords1);
                                return JsonConvert.DeserializeObject<CloudTrailRecords>(serializedRecords1);
                            }
                        }
                        catch (Exception ex)
                        {
                            var e1 = new
                            {
                                Error = $"Could not process {s}",
                                Exception = ex
                            };
                            context.Logger.LogLine(JsonConvert.SerializeObject(e1));
                            return new CloudTrailRecords();
                        }
                    });
                });
            
            var records = await Task.WhenAll(messages);
            return records.SelectMany(t => t.Records).ToList();
        }

        private class CloudTrailMessage
        {
            public string S3Bucket { get; set; }
            public string[] S3ObjectKey { get; set; }
        }

        private class CloudTrailRecords
        {
            public CloudTrailEvent[] Records { get; set; } = new CloudTrailEvent[0];
        }
    }

    public class CloudTrailEvent
    {
        public string EventSource { get; set; }
        public string EventName { get; set; }
    }
}