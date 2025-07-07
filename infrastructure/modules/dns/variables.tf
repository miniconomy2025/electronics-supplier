variable "zone_id" {
  description = "Route53 Hosted Zone ID"
  type        = string
}

variable "frontend_domain" {
  description = "Frontend domain name"
  type        = string
}

variable "frontend_target" {
  description = "CloudFront domain name"
  type        = string
}

variable "api_domain" {
  description = "API domain name"
  type        = string
}

variable "api_target" {
  description = "API CNAME target (EC2 public DNS or ELB DNS)"
  type        = string
} 