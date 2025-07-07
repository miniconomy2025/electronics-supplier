output "public_ips" {
  value = aws_eip.this[*].public_ip
  description = "Elastic IP addresses of all EC2 instances"
}

output "public_dns" {
  value = aws_eip.this[*].public_dns
  description = "Public DNS names of all EC2 instances"
}

output "ec2_instance_id" {
  value = try(aws_instance.this[*].id, null)
  description = "ID of the first EC2 instance (if any)"
}