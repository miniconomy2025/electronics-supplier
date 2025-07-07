resource "aws_sqs_queue" "main_queue" {
  name = "${var.project_prefix}-${var.queue_name}-queue"
  tags = var.common_tags
}