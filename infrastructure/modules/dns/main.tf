resource "aws_route53_record" "frontend" {
  zone_id = var.zone_id
  name    = var.frontend_domain
  type    = "CNAME"
  ttl     = 300
  records = [var.frontend_target]
}

resource "aws_route53_record" "api" {
  zone_id = var.zone_id
  name    = var.api_domain
  type    = "CNAME"
  ttl     = 300
  records = [var.api_target]
} 