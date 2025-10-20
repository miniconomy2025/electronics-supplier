# Configuration Guide

## Overview

This document provides comprehensive information about all configuration options available in the Electronics Supplier API.

## Configuration Sources (Priority Order)

1. **Environment Variables** (highest priority)
2. **Command Line Arguments**
3. **appsettings.{Environment}.json**
4. **appsettings.json** (lowest priority)

## Configuration Sections

### 1. Database Configuration

Controls database connectivity settings.

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=es_db;Username=postgres;Password="
  }
}
```

**Environment Variables:**

- `DB_PASSWORD` - Database password (appended to connection string)

**Note:** Database password should be provided via `DB_PASSWORD` environment variable for security.

### 2. External APIs Configuration

Configures endpoints for external service integrations.

```json
{
  "ExternalApis": {
    "CommercialBank": "https://commercial-bank-api.subspace.site/api",
    "BulkLogistics": "https://team7-todo.xyz/api",
    "THOH": "https://ec2-13-244-65-62.af-south-1.compute.amazonaws.com",
    "Recycler": "https://api.recycler.susnet.co.za",
    "ClientId": "electronics-supplier"
  }
}
```

**Environment Variables:**

- `ExternalApis__CommercialBank`
- `ExternalApis__BulkLogistics`
- `ExternalApis__THOH`
- `ExternalApis__Recycler`
- `ExternalApis__ClientId`

### 3. CORS Configuration

Controls cross-origin resource sharing settings.

```json
{
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "http://localhost:5173",
      "https://localhost:7000"
    ],
    "AllowCredentials": false,
    "AllowedMethods": ["GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS"],
    "AllowedHeaders": ["Content-Type", "Authorization", "Client-Id"]
  }
}
```

**Environment Variables:**

- `Cors__AllowedOrigins__0`, `Cors__AllowedOrigins__1`, etc.
- `Cors__AllowCredentials`

### 4. Simulation Configuration

Controls simulation behavior and timing.

```json
{
  "Simulation": {
    "AutoAdvanceEnabled": false
  }
}
```

**Environment Variables:**

- `Simulation__AutoAdvanceEnabled`

### 5. AWS Configuration

Configures AWS services (optional).

```json
{
  "AWS": {
    "Region": "af-south-1"
  },
  "Retry": {
    "QueueUrl": "https://sqs.af-south-1.amazonaws.com/179530787873/my-queue.fifo"
  }
}
```

**Environment Variables:**

- `AWS_REGION`
- `AWS_ACCESS_KEY_ID` (for credentials)
- `AWS_SECRET_ACCESS_KEY` (for credentials)
- `Retry__QueueUrl`

### 6. Inventory Management Configuration

Controls inventory monitoring and reordering.

```json
{
  "Inventory": {
    "MonitoredMaterials": [
      {
        "Name": "copper",
        "LowStockThreshold": 100,
        "ReorderAmount": 500
      },
      {
        "Name": "silicon", 
        "LowStockThreshold": 50,
        "ReorderAmount": 200
      }
    ]
  }
}
```

## Environment-Specific Configuration

### Development Environment

- Uses `appsettings.Development.json`
- Enables detailed logging
- Allows permissive CORS for development tools
- Database typically points to local PostgreSQL

### Production Environment  

- Uses `appsettings.Production.json`
- Reduced logging verbosity
- Strict CORS policies
- Environment variables for sensitive data

## Security Considerations

### Secrets Management

- **Never commit passwords or API keys to source control**
- Use environment variables for sensitive configuration
- Consider Azure Key Vault, AWS Secrets Manager, or similar for production

### Database Security

```bash
# Set database password via environment variable
export DB_PASSWORD="your-secure-password"

# Or in PowerShell
$env:DB_PASSWORD="your-secure-password"
```

### API Keys and External Services

```bash
# Override external API endpoints
export ExternalApis__CommercialBank="https://prod-bank.com/api"
export ExternalApis__BulkLogistics="https://prod-logistics.com/api"
```

## Docker Configuration

### Environment File (.env)

```env
DB_PASSWORD=secure-password
ExternalApis__CommercialBank=https://prod-bank.com
Simulation__AutoAdvanceEnabled=true
```

### Docker Compose

```yaml
services:
  api:
    environment:
      - DB_PASSWORD=${DB_PASSWORD}
      - ExternalApis__CommercialBank=${BANK_API_URL}
```

## Validation

The application performs startup validation for:

- Required external API endpoints
- Database connectivity
- AWS credentials (if AWS features are enabled)

Missing required configuration will cause startup failure with descriptive error messages.

## Troubleshooting

### Common Issues

1. **"External API configuration is incomplete"** - Check that all ExternalApis section values are set
2. **Database connection errors** - Verify DB_PASSWORD environment variable is set
3. **CORS errors** - Check AllowedOrigins includes your frontend URL
4. **AWS service errors** - Verify AWS credentials and region configuration

### Debug Configuration

Enable detailed configuration logging:

```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.Extensions.Configuration": "Debug"
    }
  }
}
```
