variable "sns_topic_arn" {
  description = "The ARN of the SNS topic that will be the source."
  type        = string
}

variable "sqs_subscriber_queues" {
  description = "A map where keys are descriptive names and values are the SQS queue ARNs to subscribe."
  type        = map(string)
}

variable "sqs_queue_urls" {
  description = "A map where keys match sqs_subscriber_queues and values are the SQS queue URLs (for policy attachment)."
  type        = map(string)
}