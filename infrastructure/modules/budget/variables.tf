variable "project_name" {
  description = "Project name prefix for resources"
  type        = string
}

variable "budget_emails" {
  description = "List of email addresses to receive budget alerts"
  type        = list(string)
} 