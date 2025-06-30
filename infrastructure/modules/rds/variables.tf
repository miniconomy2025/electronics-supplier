variable "project_name" {
  description = "Project name prefix for resources"
  type        = string
}

variable "subnet_ids" {
  description = "List of subnet IDs for the DB subnet group"
  type        = list(string)
}

variable "vpc_security_group_ids" {
  description = "List of VPC security group IDs for the DB"
  type        = list(string)
}

variable "aws_region" {
  description = "AWS region to deploy resources"
  type        = string
}

variable "enabled" {
  description = "Whether to create the RDS instance"
  type        = bool
  default     = true
}

variable "db_password" {
  description = "Password for the PostgreSQL master user. Set via tfvars or TF_VAR_db_password environment variable."
  type        = string
  sensitive   = true
}
variable "db_username" {
  description = "Username for the PostgreSQL master user. Set via tfvars or TF_VAR_db_username environment variable."
  type        = string
}

variable "db_name" {
  description = "The name of the database"
  type        = string
}

variable "publicly_accessible" {
  description = "Whether the RDS instance should be publicly accessible"
  type        = bool
  default     = false
}