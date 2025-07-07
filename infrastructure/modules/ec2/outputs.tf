output "public_ip" {
  value = try(aws_instance.this[0].public_ip, null)
  description = "Public IP of the first EC2 instance (if any)"
}

output "elastic_ip" {
  value = try(aws_eip.this[0].public_ip, null)
  description = "Elastic IP address of the first EC2 instance (if any)"
}

output "public_dns" {
  value = try(aws_instance.this[0].public_dns, null)
  description = "Public DNS of the first EC2 instance (if any)"
}

output "ec2_instance_id" {
  value = try(aws_instance.this[0].id, null)
  description = "ID of the first EC2 instance (if any)"
}