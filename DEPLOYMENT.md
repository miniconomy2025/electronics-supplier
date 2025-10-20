# Deployment Setup Guide

## Quick Setup for Automated Deployment

### 1. Required GitHub Repository Secrets

Go to your GitHub repository → Settings → Secrets and variables → Actions, then add:

```env
EC2_HOST = your-ec2-public-ip-or-hostname
EC2_PRIVATE_KEY = [paste your EC2 private key content here]
```

**Optional (for AWS services):**

```env
AWS_ACCESS_KEY_ID = your-aws-access-key
AWS_SECRET_ACCESS_KEY = your-aws-secret-key
```

### 2. EC2 Prerequisites

Your EC2 instance should have:

- ✅ .NET 8 SDK installed
- ✅ Git repository cloned to `/home/ubuntu/electronics-supplier/`
- ✅ SSH access configured
- ✅ Port 5062 open for the application

### 3. Current Configuration

The deployment is configured to match your current EC2 setup:

- **Environment**: Development (to match your current launchSettings.json)
- **Database**: RDS `es-db.cnhrl5wk3uki.af-south-1.rds.amazonaws.com`
- **Application URL**: `http://0.0.0.0:5062`
- **tmux Session**: `electronics-supplier-api`

### 4. How It Works

1. **Trigger**: Push to `main` branch
2. **Pre-deployment**: Run tests and build validation
3. **Deploy**: SSH to EC2 and run deployment script
4. **Configuration**: Copy production settings (RDS connection, external APIs)
5. **Application**: Start in tmux with Development environment but production database
6. **Health Check**: Verify application is running correctly
7. **Rollback**: Automatic rollback if deployment fails

### 5. Manual Deployment

If you need to deploy manually:

```bash
# SSH to your EC2 instance
ssh -i your-key.pem ubuntu@your-ec2-host

# Navigate to the application directory
cd /home/ubuntu/electronics-supplier

# Run the deployment script
./deploy.sh
```

### 6. Monitoring

After deployment, you can:

```bash
# Check tmux session
tmux attach-session -t electronics-supplier-api

# View application logs
tmux capture-pane -t electronics-supplier-api -p

# Check application health
curl http://localhost:5062/health
```

### 7. Configuration Files

- **`appsettings.Production.json`**: Contains your real RDS connection and external API endpoints
- **`launchSettings.Production.json`**: Network binding configuration for EC2
- **`deploy.sh`**: Main deployment script with backup/rollback capabilities

The deployment process preserves your current environment setup while ensuring the latest code and production configuration are deployed safely.
