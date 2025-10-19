provider "aws" {
  region = var.aws_region
}

module "vpc" {
  source       = "./modules/vpc"
  project_name = var.project_name
  vpc_cidr     = var.vpc_cidr
  aws_region   = var.aws_region
  public_subnet_count = 2
}

module "ec2" {
  source         = "./modules/ec2"
  project_name   = var.project_name
  subnet_ids     = [module.vpc.public_subnet_id]
  instance_count = var.ec2_instance_count
  aws_region     = var.aws_region
  security_group_id = module.vpc.default_security_group_id
  key_name = var.key_name
}

module "rds" {
  source                 = "./modules/rds"
  project_name           = var.project_name
  subnet_ids             = module.vpc.private_subnet_ids
  vpc_security_group_ids = [module.vpc.default_security_group_id]
  aws_region             = var.aws_region
  enabled                = var.rds_enabled
  db_password            = var.db_password
  db_username            = var.db_username
  db_name                = var.db_name
  publicly_accessible    = false
}

module "budget" {
  source       = "./modules/budget"
  project_name = var.project_name
  budget_emails = var.budget_emails
}

resource "aws_sns_topic" "bank_payment_notification_topic" {
  name = "${var.project_name}-bank-payment-notification-topic"
  tags = var.common_tags
}

module "payment_confirmation_queue" {
  source         = "./modules/sqs-basic"
  project_prefix = var.project_name
  queue_name     = "payment-confirmation"
  common_tags    = var.common_tags
}


module "payment_notification_fanout" {
  source        = "./modules/sns-to-sqs-fanout"
  sns_topic_arn = aws_sns_topic.bank_payment_notification_topic.arn

  sqs_subscriber_queues = {
    payment_confirmation = module.payment_confirmation_queue.queue_arn
  }

  sqs_queue_urls = {
    payment_confirmation = module.payment_confirmation_queue.queue_id
  }
}
