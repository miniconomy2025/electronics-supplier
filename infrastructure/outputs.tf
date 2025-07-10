output "vpc_id" {
  value = module.vpc.vpc_id
}

output "public_subnet_id" {
  value = module.vpc.public_subnet_id
}

output "ec2_public_ip" {
  value = module.ec2.public_ip
}

output "rds_endpoint" {
  value       = module.rds.endpoint
  description = "RDS endpoint"
}

output "ec2_elastic_ip" {
  value = module.ec2.elastic_ip
  description = "Elastic IP address of the first EC2 instance (if any)"
}

output "ec2_public_dns" {
  value       = module.ec2.public_dns
  description = "Public DNS of the first EC2 instance"
}

output "sns_order_placed_topic_arn" {
  description = "The ARN of the SNS topic for placed material orders."
  value       = aws_sns_topic.bank_payment_notification_topic.arn
}

output "sqs_supplier_order_queue_url" {
  description = "The URL of the SQS queue for processing supplier orders."
  value       = module.payment_confirmation_queue.queue_id
}