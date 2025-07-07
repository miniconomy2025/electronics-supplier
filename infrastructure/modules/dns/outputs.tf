output "frontend_record_fqdn" {
  value = aws_route53_record.frontend.fqdn
}

output "api_record_fqdn" {
  value = aws_route53_record.api.fqdn
} 