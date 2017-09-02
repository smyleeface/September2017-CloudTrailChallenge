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
            // ### Level 1 - Create New Trail and Configure Lambda
            var messages = evnt.Records?.Select(r=> r.Sns?.Message).Where(s => !string.IsNullOrWhiteSpace(s)) ?? 
                Enumerable.Empty<string>();
            foreach (var message in messages)
            {
                context.Logger.LogLine(message);
            }
        }
    }
}
