variable "project_name" {
  description = "Project name prefix for resources"
  type        = string
}

variable "domain_name" {
  description = "Domain name for the frontend (e.g., electronics-supplier.projects.bbdgrad.com)"
  type        = string
}

variable "aws_region" {
  description = "AWS region for ACM certificate (must be us-east-1 for CloudFront)"
  type        = string
  default     = "af-south-1"
} 