# λ# Find and fix security problems automatically with CloudTrail and Lambda - September 2017 Team Hackathon Challenge 

> AWS CloudTrail Event History is now available to all customers. CloudTrail Event History (formerly known as API Activity History) lets you view, search, and download your recent AWS account activity. This allows you to gain visibility into your account actions taken through the AWS Management Console, SDKs, and CLI to enable governance, compliance, and operational and risk auditing of your AWS account.

* [AWS CloudTrail Event History Now Available to All Customers](https://aws.amazon.com/about-aws/whats-new/2017/08/aws-cloudtrail-event-history-now-available-to-all-customers/) Posted On: Aug 14, 2017

## Pre-requisites - Do this before the hackathon

The following tools and accounts are required to complete these instructions.

* [Complete Step 1 of the AWS Lambda Getting Started Guide](http://docs.aws.amazon.com/lambda/latest/dg/setup.html)
  * Setup an AWS account
  * Setup the AWS CLI
* Create a CLI profile
  * Use `aws configure --profile lambdasharp` to create a profile to use with the example scripts.
  * Provide an AWS Access Key ID and the corresponding Secret Key
  * `us-west-2` is a good choice for region
  * Leave default output format as `None` it doesn't matter.
* [Install .NET Core 1.0.x](https://github.com/dotnet/core/blob/master/release-notes/download-archives/1.0.5-download.md)

## LEVEL 0 - Setup

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

## Level 1 - Create New Trail and Configure Lambda

In this section you will:

* Use the `create-subscription` command to setup your cloudtrail infrastructure

* Create an S3 Bucket to receive logs
* Apply a bucket policy that allows CloudTrail to write logs to your bucket
* Create a new CloudTrail trail
* Start logging for the new CloudTrail trail
* Configure SNS notifications for CloudTrail

* Create an IAM role to manage permissions for your lambda function
* Add permissions to the lambda execution role
* Deploy a lambda to receive CloudTrail SNS notifications
* Configure an SNS topic trigger for your lambda
* Give SNS permission to invoke your lambda
* See the lambda create a CloudWatch log entry each time CloudTrail sends a notification

### Use the `create-subscription` command to setup your cloudtrail infrastructure

`create-subscription` is a high-level `cloudtrail` command that does alot of the necessary configuration for you.

Run this command (replace `<team#>` with a teamid. example: `team0`):

```bash
aws cloudtrail create-subscription --s3-new-bucket lambdasharp-<team#>-cloudtrail --sns-new-topic <team#>-cloudtrail-logs --name <team#>-trail --profile lambdasharp
```

You should see a response like this:

  ```bash
Setting up new S3 bucket lambdasharp-team0-cloudtrail...
Setting up new SNS topic team0-cloudtrail-logs...
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
    "RequestId": "933da745-92b0-11e7-8b1d-370acec13ecd",
    "HTTPHeaders": {
      "x-amzn-requestid": "933da745-92b0-11e7-8b1d-370acec13ecd",
      "date": "Wed, 06 Sep 2017 03:07:53 GMT",
      "content-length": "420",
      "content-type": "application/x-amz-json-1.1"
    }
  }
}
Starting CloudTrail service...
Logs will be delivered to lambdasharp-team0-cloudtrail:
```

### Create an IAM role to manage permissions for you lambda function

> Note: you must execute this command from the repository root for the `file://` path to find the `role-trust-policy.json` file.

You can create the role using the `create-role` command:

```bash
aws iam create-role --role-name cloudtrailer-lambda-role --assume-role-policy-document file://support/role-trust-policy.json --profile lambdasharp
```

This command produces output that looks like this:

```json
{
    "Role": {
        "AssumeRolePolicyDocument": {
            "Version": "2012-10-17",
            "Statement": [
                {
                    "Action": "sts:AssumeRole",
                    "Effect": "Allow",
                    "Principal": {
                        "Service": "lambda.amazonaws.com"
                    }
                }
            ]
        },
        "RoleId": "AROAJVBX5QQ2HYX3FV72S",
        "CreateDate": "2017-09-02T14:19:35.376Z",
        "RoleName": "team0-lambda-role",
        "Path": "/",
        "Arn": "arn:aws:iam::############:role/cloudtrailer-lambda-role"
    }
}
```

### Add permissions to the lambda execution role

> Note: you must execute this command from the repository root for the `file://` path to find the `role-trust-policy.json` file.

Edit `support/role-trust-policy.json` to make sure your s3 bucket ARN matches the ARN for your team bucket.

You can update the role permissions using the `put-role-policy` command:

```bash
aws iam put-role-policy --role-name cloudtrailer-lambda-role --policy-name execution-policy --policy-document file://support/role-execution-policy.json --profile lambdasharp
```

This command produces no output on success.

### Deploy a lambda to receive CloudTrail SNS notifications

> Note: for this command to work you must be in the `src/CloudTrailer` folder.

You can deploy the provided lambda with this command:

```bash
dotnet lambda deploy-function <team#>-cloudtrailer
```

This command produces output that looks like this:

```text
Executing publish command
Deleted previous publish folder
... invoking 'dotnet publish', working folder '.../September2017-CloudTrailChallenge/src/CloudTrailer/bin/Release/netcoreapp1.0/publish'
... publish: Microsoft (R) Build Engine version 15.3.409.57025 for .NET Core
... publish: Copyright (C) Microsoft Corporation. All rights reserved.
... publish:   CloudTrailer -> .../September2017-CloudTrailChallenge/src/CloudTrailer/bin/Release/netcoreapp1.0/CloudTrailer.dll
... publish:   CloudTrailer -> .../September2017-CloudTrailChallenge/src/CloudTrailer/bin/Release/netcoreapp1.0/publish/
Changed permissions on published dll (chmod +r Amazon.Lambda.Core.dll).
Changed permissions on published dll (chmod +r Amazon.Lambda.Serialization.Json.dll).
Changed permissions on published dll (chmod +r Amazon.Lambda.SNSEvents.dll).
Changed permissions on published dll (chmod +r AWSSDK.Core.dll).
Changed permissions on published dll (chmod +r AWSSDK.SimpleNotificationService.dll).
Changed permissions on published dll (chmod +r CloudTrailer.dll).
Changed permissions on published dll (chmod +r Newtonsoft.Json.dll).
Changed permissions on published dll (chmod +r System.Runtime.Serialization.Primitives.dll).
Zipping publish folder .../September2017-CloudTrailChallenge/src/CloudTrailer/bin/Release/netcoreapp1.0/publish to .../September2017-CloudTrailChallenge/src/CloudTrailer/bin/Release/netcoreapp1.0/CloudTrailer.zip
... zipping:   adding: Amazon.Lambda.Core.dll (deflated 57%)
... zipping:   adding: Amazon.Lambda.Serialization.Json.dll (deflated 56%)
... zipping:   adding: Amazon.Lambda.SNSEvents.dll (deflated 60%)
... zipping:   adding: AWSSDK.Core.dll (deflated 66%)
... zipping:   adding: AWSSDK.SimpleNotificationService.dll (deflated 68%)
... zipping:   adding: CloudTrailer.deps.json (deflated 72%)
... zipping:   adding: CloudTrailer.dll (deflated 61%)
... zipping:   adding: CloudTrailer.pdb (deflated 30%)
... zipping:   adding: Newtonsoft.Json.dll (deflated 60%)
... zipping:   adding: System.Runtime.Serialization.Primitives.dll (deflated 48%)
Created publish archive (.../September2017-CloudTrailChallenge/src/CloudTrailer/bin/Release/netcoreapp1.0/CloudTrailer.zip).
Creating new Lambda function team0-cloudtrailer
New Lambda function created
```

### Configure an SNS topic trigger for your lambda

You can create a trigger for your lambda using the `sns subscribe` command.

* This command requires the sns topic arn (HINT: this was printed as part of the [create SNS notifications](#notifySns) step).

* This command requires the lambda arn. (HINT: try `aws lambda get-function --function-name <team#>-cloudtrailer --profile lambdasharp`)

```bash
aws sns subscribe --topic-arn arn:aws:sns:us-west-2:<account#>:<team#>-cloudtrail-logs --protocol lambda --notification-endpoint arn:aws:lambda:us-west-2:<account#>:function:<team#>-cloudtrailer --profile lambdasharp
```

This command produces output that looks like this:

```json
{
    "SubscriptionArn": "arn:aws:sns:us-west-2:############:team0-cloudtrail-logs:02be1b87-7d1b-4772-93a9-7a15e2484087"
}
```

### Give SNS permission to invoke your lambda

You can give SNS permission to invoke your lambda using the `add-permission` command.

* This command requires the sns topic arn (HINT: this was printed as part of the [create SNS notifications](#notifySns) step).

```bash
aws lambda add-permission --function-name <team#>-cloudtrailer --statement-id 1 --action lambda:InvokeFunction --principal sns.amazonaws.com --source-arn arn:aws:sns:us-west-2:<account#>:<team#>-cloudtrail-logs --profile lambdasharp
```

This command produces output that looks like this:

```json
{
    "Statement": "{\"Sid\":\"1\",\"Effect\":\"Allow\",\"Principal\":{\"Service\":\"sns.amazonaws.com\"},\"Action\":\"lambda:InvokeFunction\",\"Resource\":\"arn:aws:lambda:us-west-2:############:function:team0-cloudtrailer\",\"Condition\":{\"ArnLike\":{\"AWS:SourceArn\":\"arn:aws:sns:us-west-2:############:team0-cloudtrail-logs\"}}}"
}
```

### See the lambda create a CloudWatch log entry each time CloudTrail sends a notification

Because of the delay associated with CloudTrail, you may see events associated with your work up until now delivered within a few minutes.  However, to be on the safe side, create another user now, which will guarantee that you get an event to work with:

```bash
aws iam create-user --user-name Alice --profile lambdasharp
```

On the command line, this command should create output similar to the output you saw when creating `Bob`.

If your lambda is set up correctly then you should see an invocation for this event and the lambda should output the event notification to CloudWatch.

> Hint: Once you see your first notification event in the log, you may save a lot of time by copying it and using it as test input to your lambda.  Then you will not have to wait for the CloudTrail logging and delivery delays each time you want to test.

## LEVEL 2 - Retrieve Logs from S3

Update your lambda to:

* Parse the incoming notification message to see the CloudTrail messages, which will tell you the bucket and key(s) for the logs which CloudTrail has shipped to S3.

* Retrieve the logs from S3

* Write the logs to CloudWatch

## LEVEL 3 - Filter for specific events and send alerts

Update your infrastructure:

* Create a new SNS topic to publish filtered events to

* Subscribe to this topic so that you are alerted when a filtered event is detected (Hint: SMS or Email are good choices to explore)

* Grant lambda permission to publish to the topic

Update your lambda:

* Filter the logged events for events may be interesting from a security perspective.

* Publish filtered events to your new SNS topic

## Boss level - Take mitigating action

Now that you have found events that may represent security risks, use the AWSSDK to instruct AWS to correct the problems.