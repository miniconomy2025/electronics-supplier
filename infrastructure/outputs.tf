output "vpc_id" {
  value = module.vpc.vpc_id
}

output "public_subnet_ids" {
  value = module.vpc.public_subnet_ids
}

output "rds_endpoint" {
  value       = module.rds.endpoint
  description = "RDS endpoint"
}

output "ec2_public_ips" {
  value = module.ec2.public_ips
}

output "ec2_public_dns" {
  value = module.ec2.public_dns
}