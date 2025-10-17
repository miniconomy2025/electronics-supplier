output "vpc_id" {
  value = aws_vpc.this.id
}

output "public_subnet_id" {
  value = aws_subnet.public[0].id
}

output "private_subnet_ids" {
  value = [for s in aws_subnet.private : s.id]
}

output "default_security_group_id" {
  value = aws_security_group.default.id
}
