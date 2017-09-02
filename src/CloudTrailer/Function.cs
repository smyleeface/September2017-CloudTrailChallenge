using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using Amazon.SimpleNotificationService;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace CloudTrailer
{
    public class Function
    {
        private IAmazonSimpleNotificationService SnsClient { get; }

        /// <summary>
        /// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
        /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
        /// region the Lambda function is executed in.
        /// </summary>
        public Function()
        {
            SnsClient = new AmazonSimpleNotificationServiceClient();
        }

        /// <summary>
        /// Constructs an instance with a preconfigured client. This can be used for testing the outside of the Lambda environment.
        /// </summary>
        /// <param name="snsClient">The client</param>
        public Function(IAmazonSimpleNotificationService snsClient)
        {
            SnsClient = snsClient;
        }
        
        public void FunctionHandler(SNSEvent evnt, ILambdaContext context)
        {
            var snsEvent = evnt.Records?[0].Sns;
            if(snsEvent == null)
            {
                return;
            }

            try
            {
//                {
//                    "s3Bucket": "your-bucket-name","s3ObjectKey": ["AWSLogs/123456789012/CloudTrail/us-east-2/2013/12/13/123456789012_CloudTrail_us-west-2_20131213T1920Z_LnPgDQnpkSKEsppV.json.gz"]
//                }
                // var response = await this.S3Client.GetObjectMetadataAsync(snsEvent.Bucket.Name, snsEvent.Object.Key);
                // return response.Headers.ContentType;
            }
            catch(Exception e)
            {
                // context.Logger.LogLine($"Error getting object {snsEvent.Object.Key} from bucket {snsEvent.Bucket.Name}. Make sure they exist and your bucket is in the same region as this function.");
                // context.Logger.LogLine(e.Message);
                // context.Logger.LogLine(e.StackTrace);
                throw;
            }
        }
    }
}
