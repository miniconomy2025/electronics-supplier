resource "aws_budgets_budget" "monthly_budget" {
  name              = "${var.project_name}-monthly-budget"
  budget_type       = "COST"
  limit_amount      = "25"
  limit_unit        = "USD"
  time_unit         = "MONTHLY"

  notification {
    comparison_operator = "GREATER_THAN"
    notification_type   = "FORECASTED"
    threshold           = 50
    threshold_type      = "PERCENTAGE"
    subscriber_email_addresses = var.budget_emails
  }

  notification {
    comparison_operator = "GREATER_THAN"
    notification_type   = "FORECASTED"
    threshold           = 75
    threshold_type      = "PERCENTAGE"
    subscriber_email_addresses = var.budget_emails
  }

  dynamic "notification" {
    for_each = [for i in range(2, 10) : i * 10]
    content {
      comparison_operator = "GREATER_THAN"
      notification_type   = "ACTUAL"
      threshold           = notification.value
      threshold_type      = "PERCENTAGE"
      subscriber_email_addresses = var.budget_emails
    }
  }
} 