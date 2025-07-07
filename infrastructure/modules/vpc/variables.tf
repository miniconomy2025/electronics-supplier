variable "project_name" {
  description = "Project name prefix for resources"
  type        = string
}

variable "vpc_cidr" {
  description = "CIDR block for the VPC"
  type        = string
}

variable "aws_region" {
  description = "AWS region to deploy resources"
  type        = string
}

variable "private_subnet_count" {
  description = "Number of private subnets (and AZs) to create"
  type        = number
  default     = 2
}

variable "public_subnet_count" {
  description = "Number of public subnets (and AZs) to create"
  type        = number
  default     = 2
}