resource "aws_instance" "this" {
  count         = var.instance_count
  ami           = data.aws_ami.ubuntu.id
  instance_type = "t3.micro"
  subnet_id     = element(var.subnet_ids, count.index % length(var.subnet_ids))
  vpc_security_group_ids = [var.security_group_id]
  tags = {
    Name = "${var.project_name}-ec2-${count.index}"
  }
  user_data = <<-EOF
    #!/bin/bash
    # Updating system and installing dependencies
    apt-get update -y
    apt-get install -y curl git

    # Installing Node.js (LTS)
    curl -fsSL https://deb.nodesource.com/setup_lts.x | bash -
    apt-get install -y nodejs

    # Installing PM2, TypeScript, and ts-node globally
    npm install -g pm2 typescript ts-node

    # Cloning the repo
    git clone {repo link} /home/ubuntu/{project name}

    # Setting permissions
    chown -R ubuntu:ubuntu /home/ubuntu/{project name}

    # Installing dependencies
    cd /home/ubuntu/{project name}
    sudo -u ubuntu npm install

    # Start the app with PM2
    sudo -u ubuntu pm2 start dist/app.js --name {project name}

    # Set PM2 to start on boot
    sudo -u ubuntu pm2 startup systemd -u ubuntu --hp /home/ubuntu
    sudo -u ubuntu pm2 save

  EOF
  iam_instance_profile = var.iam_instance_profile
  key_name = var.key_name
}

data "aws_ami" "ubuntu" {
  most_recent = true
  owners      = ["099720109477"] # Canonical
  filter {
    name   = "name"
    values = ["ubuntu/images/hvm-ssd/ubuntu-*-amd64-server-*"]
  }
}

resource "aws_eip" "this" {
  count = var.instance_count > 0 ? 1 : 0
  instance = aws_instance.this[0].id
  domain   = "vpc"
}