# λ# Find and fix security problems automatically with CloudTrail and Lambda - September 2017 Team Hackathon Challenge 

> AWS CloudTrail Event History is now available to all customers. CloudTrail Event History (formerly known as API Activity History) lets you view, search, and download your recent AWS account activity. This allows you to gain visibility into your account actions taken through the AWS Management Console, SDKs, and CLI to enable governance, compliance, and operational and risk auditing of your AWS account.

* [AWS CloudTrail Event History Now Available to All Customers](https://aws.amazon.com/about-aws/whats-new/2017/08/aws-cloudtrail-event-history-now-available-to-all-customers/) Posted On: Aug 14, 2017

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

### Level 1 - Create New Trail and Configure Lambda

In this section you will:

* Create an S3 Bucket to recieve logs
* Apply a bucket policy that allows CloudTrail to write logs to your bucket
* Create a new CloudTrail trail
* Start logging for the new CloudTrail trail
* Configure SNS notifications for CloudTrail

#### Create S3 Bucket

Create a bucket to receive logs using this command (replace `<team#>` with a teamid. example: `team0`):

```bash
aws s3api create-bucket --bucket lambdasharp-<team#>-cloudtrail --region us-west-2 --profile lambdasharp
```

You should see a response like this:

```json
{
    "Location": "/lambdasharp-team0-cloudtrail"
}
```

#### Apply bucket policy

> Note: you must execute this command from the repository root for the `file://` path to find the `bucket-policy.json` file.

* Update `support/bucket-policy.json` to replace `<team#>` with the teamid you picked when creating the bucket.

* Run the `put-bucket-policy` command to apply the policy to the bucket.
  ```bash
  aws s3api put-bucket-policy --bucket lambdasharp-<team#>-cloudtrail --policy file://support/bucket-policy.json --profile lambdasharp
  ```
* This command has no output on success (yay).

#### Create a new CloudTrail trail

Now you can create the new trail with the `create-trail` command:

```bash
aws cloudtrail create-trail --name <team#>-trail --s3-bucket-name lambdasharp-<team#>-cloudtrail --profile lambdasharp
```

You should see output like this on success:

```json
{
    "IncludeGlobalServiceEvents": true,
    "Name": "team0-trail",
    "TrailARN": "arn:aws:cloudtrail:us-west-2:############:trail/team0-trail",
    "LogFileValidationEnabled": false,
    "IsMultiRegionTrail": false,
    "S3BucketName": "lambdasharp-team0-cloudtrail"
}
```

You should also see the new trail [in the CloudTrail console](https://us-west-2.console.aws.amazon.com/cloudtrail/home?region=us-west-2#/configuration)

#### Start logging

The `create-trail` command does not turn on logging when it creates the trail.  Use the `start-logging` command to start logging:

```bash
aws cloudtrail start-logging --name <team#>-trail --profile lambdasharp
```

This command produces no output on success, however you can run the `get-trail-status` command to check that logging has started.

```bash
aws cloudtrail get-trail-status --name <team#>-trail --profile lambdasharp
```

Which produces output like this:

```json
{
    "LatestDeliveryAttemptTime": "",
    "LatestNotificationAttemptSucceeded": "",
    "LatestDeliveryAttemptSucceeded": "",
    "IsLogging": true,
    "TimeLoggingStarted": "2017-08-30T14:24:41Z",
    "StartLoggingTime": 1504103081.927,
    "LatestNotificationAttemptTime": "",
    "TimeLoggingStopped": ""
}
```

When there is a value for `LatestNotificationAttemptSucceeded` it means that the trail has written logs to your S3 bucket. According to the CloudTrail FAQ log files are delivered every 5 minutes, keep in mind that the API event has to have reached CloudTrail before it will be published, so the total wait time could be as long as 20 minutes.  Keep working and check back for logs every few minutes if you don't see them right away.

Finally you can see the event selectors using the `get-event-selectors` command:

```bash
aws cloudtrail get-event-selectors --trail-name <team#>-trail --profile lambdasharp
```

Which produces output like this:

```json
{
    "EventSelectors": [
        {
            "IncludeManagementEvents": true,
            "DataResources": [],
            "ReadWriteType": "All"
        }
    ],
    "TrailARN": "arn:aws:cloudtrail:us-west-2:############:trail/team0-trail"
}
```

Management events are included by default. Data events are not.  Data events are used to audit access to S3 objects, all other API activity (such as creating a CloutTrail trail, or checking its status) in AWS is a management event.

#### Configure SNS notifications for CloudTrail

You can create the SNS topic using the `create-subscription` command.

```bash
aws cloudtrail create-subscription --name <team#>-trail --s3-use-bucket lambdasharp-<team#>-cloudtrail --sns-new-topic <team#>-cloudtrail-logs --profile lambdasharp
```

This command produces output that looks like this:

```json
Creating/updating CloudTrail configuration...
CloudTrail configuration:
{
  "trailList": [
    {
      "IncludeGlobalServiceEvents": true,
      "Name": "team0-trail",
      "TrailARN": "arn:aws:cloudtrail:us-west-2:############:trail/team0-trail",
      "LogFileValidationEnabled": false,
      "SnsTopicARN": "arn:aws:sns:us-west-2:############:team0-cloudtrail-logs",
      "IsMultiRegionTrail": false,
      "HasCustomEventSelectors": false,
      "S3BucketName": "lambdasharp-team0-cloudtrail",
      "SnsTopicName": "team0-cloudtrail-logs",
      "HomeRegion": "us-west-2"
    }
  ],
  "ResponseMetadata": {
    "RetryAttempts": 0,
    "HTTPStatusCode": 200,
    "RequestId": "da3b6dfb-8d99-11e7-92c7-73b82291b0fa",
    "HTTPHeaders": {
      "x-amzn-requestid": "da3b6dfb-8d99-11e7-92c7-73b82291b0fa",
      "date": "Wed, 30 Aug 2017 15:42:38 GMT",
      "content-length": "420",
      "content-type": "application/x-amz-json-1.1"
    }
  }
}
Starting CloudTrail service...
Logs will be delivered to lambdasharp-team0-cloudtrail:
```

TODO:  lambda.