output "cloudfront_domain_name" {
  value = aws_cloudfront_distribution.frontend.domain_name
}

output "acm_certificate_arn" {
  value = aws_acm_certificate.frontend.arn
}

output "s3_bucket_name" {
  value = aws_s3_bucket.frontend.bucket
} 