resource "aws_sns_topic_subscription" "subscriptions" {
  for_each  = var.sqs_subscriber_queues
  topic_arn = var.sns_topic_arn
  protocol  = "sqs"
  endpoint  = each.value
}

data "aws_iam_policy_document" "sns_to_sqs_policy_document" {
  for_each = var.sqs_subscriber_queues

  statement {
    effect    = "Allow"
    actions   = ["SQS:SendMessage"]
    resources = [each.value]

    principals {
      type        = "Service"
      identifiers = ["sns.amazonaws.com"]
    }

    condition {
      test     = "ArnEquals"
      variable = "aws:SourceArn"
      values   = [var.sns_topic_arn]
    }
  }
}

resource "aws_sqs_queue_policy" "queue_policies" {
  for_each = var.sqs_queue_urls
  queue_url = each.value
  policy    = data.aws_iam_policy_document.sns_to_sqs_policy_document[each.key].json
}