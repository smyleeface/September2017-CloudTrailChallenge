# λ# Find and fix security problems automatically with CloudTrail and Lambda - September 2017 Team Hackathon Challenge 

> AWS CloudTrail Event History is now available to all customers. CloudTrail Event History (formerly known as API Activity History) lets you view, search, and download your recent AWS account activity. This allows you to gain visibility into your account actions taken through the AWS Management Console, SDKs, and CLI to enable governance, compliance, and operational and risk auditing of your AWS account.

- [AWS CloudTrail Event History Now Available to All Customers](https://aws.amazon.com/about-aws/whats-new/2017/08/aws-cloudtrail-event-history-now-available-to-all-customers/)
Posted On: Aug 14, 2017

## Pre-requisites

The following tools and accounts are required to complete these instructions.

* [Install .NET Core 1.0.x](https://github.com/dotnet/core/blob/master/release-notes/download-archives/1.0.5-download.md)
* [Install AWS CLI](https://aws.amazon.com/cli/)
    * Use `aws configure --profile lambdasharp` to create a profile to use with the example scripts.
    * Provide an AWS Access Key ID and the corresponding Secret Key
    * `us-west-2` is a good choice for region
    * Leave default output format as `None` it doesn't matter.
* [Sign-up for an AWS account](https://aws.amazon.com/)

### LEVEL 0 - Setup

Since CloudTrail is enabled by default you should be able to see the past seven days of events just by [visiting CloudTrail](https://us-west-2.console.aws.amazon.com/cloudtrail/home?region=us-west-2#/dashboard).  However, depending on your account and the services you are using (if any) the event list may not show any events.

You can generate an envent in CloudTrail by using this script to create a new user:

```bash
aws iam create-user --user-name Bob --profile lambdasharp
```

You should see a response like this:

```json
{
    "User": {
        "UserName": "Bob", 
        "Path": "/", 
        "CreateDate": "2017-08-30T05:04:59.072Z", 
        "UserId": "AIDAJILOPLVW6D3FEN5AC", 
        "Arn": "arn:aws:iam::############:user/Bob"
    }
}
```

Now refresh your CloudTrail console, it may take a minute or two, but according to the [CloudTrail FAQ](https://aws.amazon.com/cloudtrail/faqs/) it can take up to 15 minutes for events to appear.  If you do not see the event after a few minutes, move on to the rest of the setup and check back later. 

TODO: Create a new trail, setup log shipping to s3, SNS, lambda.