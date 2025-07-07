# Electronics Supplier Infrastructure

## Structure

- `main.tf` - Root configuration, calls modules
- `variables.tf` - Input variables
- `outputs.tf` - Useful outputs
- `backend.tf` - S3 backend for remote state
- `modules/vpc` - VPC, subnets, and networking
- `modules/ec2` - EC2 instance
- `modules/rds` - RDS instance
- `modules/budget` - Budgeting alerts

## Usage

1. **Configure backend**: Edit `backend.tf` with your S3 bucket.
2. **Set variables**: Edit `terraform.tfvars` or use environment variables.
3. **Initialise and apply**:

   ```sh
   terraform init -backend-config="backed.config"
   terraform plan
   terraform apply
   ```

## Free Tier Defaults

- EC2: t3.micro
- RDS: db.t3.micro (or free-tier eligible)
- Minimal resources to avoid costs
