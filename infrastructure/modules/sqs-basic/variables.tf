variable "queue_name" {
  description = "The name suffix for the SQS queue."
  type        = string
}

variable "project_prefix" {
  description = "A prefix for all resource names."
  type        = string
}

variable "common_tags" {
  description = "Common tags to apply to all resources."
  type        = map(string)
  default     = {}
}