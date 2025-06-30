output "endpoint" {
  value = try(aws_db_instance.this[0].endpoint, null)
  description = "RDS endpoint (if created)"
}
