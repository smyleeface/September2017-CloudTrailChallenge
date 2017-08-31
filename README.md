# λ# Find and fix security problems automatically with CloudTrail and Lambda - September 2017 Team Hackathon Challenge 

`CHALLENGE OVERVIEW`

### Pre-requisites
The following tools and accounts are required to complete these instructions.

* [Install .NET Core 1.0.x](https://github.com/dotnet/core/blob/master/release-notes/download-archives/1.0.5-download.md)
* [Install AWS CLI](https://aws.amazon.com/cli/)
* [Sign-up for an AWS account](https://aws.amazon.com/)

## LEVEL 0 - Setup
The following steps will walk you through the set-up of the challenge basics.

### Create `lambdasharp` AWS Profile
The project uses by default the `lambdasharp` profile. Follow these steps to setup a new profile if need be.

1. Create a `lambdasharp` profile: `aws configure --profile lambdasharp`
2. Configure the profile with the AWS credentials and region you want to use

### Create `LambdaSharp-SentinelFunction` role for the lambda function
The `LambdaSharp-SentinelFunction` lambda function requires an IAM role. You can create the `LambdaSharp-SentinelFunction` role via the [AWS Console](https://console.aws.amazon.com/iam/home) or use the executing [AWS CLI](https://aws.amazon.com/cli/) commands.
```shell
aws iam create-role --profile lambdasharp --role-name LambdaSharp-SentinelFunction --assume-role-policy-document file://assets/lambda-role-policy.json
aws iam attach-role-policy --profile lambdasharp --role-name LambdaSharp-SentinelFunction --policy-arn arn:aws:iam::aws:policy/AWSLambdaFullAccess
```


## LEVEL 1 - CloudTrail Activation

`TODO`


## LEVEL 2 - Deploy Sentinel Function

`TODO`


## LEVEL 3 - Send Notifications

`TODO`


## BOSS LEVEL - Apply Mitigations

`TODO`


## Copyright & License
* Copyright (c) 2017 Jim Counts, Steve Bjorg
* MIT License