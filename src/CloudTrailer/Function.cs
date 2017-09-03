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
using Amazon.S3.Model;
using Newtonsoft.Json;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace CloudTrailer
{
    public class Function
    {
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
            var messages = evnt.Records?.Select(r => r.Sns?.Message).Where(s => !string.IsNullOrWhiteSpace(s)) ??
                           Enumerable.Empty<string>();
            foreach (var message in messages)
            {
                context.Logger.LogLine(message);

                var ct = JsonConvert.DeserializeObject<CloudTrailMessage>(message);
                var tasks = ct.S3ObjectKey.Select(async s =>
                {
                    try
                    {
                        var response = await S3Client.GetObjectAsync(ct.S3Bucket, s);
                        using (var contents = new MemoryStream())
                        {
                            await response.ResponseStream.CopyToAsync(contents);
                            return Encoding.UTF8.GetString(contents.ToArray());
                        }
                    }
                    catch (Exception ex)
                    {
                        var e = new
                        {
                            Error = $"Could not process {s}",
                            Exception = ex
                        };
                        return JsonConvert.SerializeObject(e);
                    }
                });
                var logs = await Task.WhenAll(tasks);
                foreach (var log in logs)
                {
                    context.Logger.LogLine(log);
                }
            }


            // ### Level 3 - Filter for specific events and send alerts

            // ### Boss level - Take mitigating action
        }

        private class CloudTrailMessage
        {
            public string S3Bucket { get; set; }
            public string[] S3ObjectKey { get; set; }
        }
    }
}