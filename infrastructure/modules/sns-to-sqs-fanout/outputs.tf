output "subscription_arns" {
  description = "A map of subscription ARNs created."
  value = {
    for k, v in aws_sns_topic_subscription.subscriptions : k => v.arn
  }
}