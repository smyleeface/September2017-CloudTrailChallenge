#! /bin/bash

echo "Creating subscription for ${1}"

ARN=`aws sns create-topic --name team0-cloudtrail-alerts --profile lambdasharp | jq -r '.TopicArn'`
echo "Created ${ARN}"

aws sns subscribe --topic-arn ${ARN} --protocol sms --notification-endpoint ${1} --profile lambdasharp

aws iam put-role-policy --role-name cloudtrailer-lambda-role --policy-name publish-alert-policy --policy-document file://support/publish-alerts-policy0.json --profile lambdasharp