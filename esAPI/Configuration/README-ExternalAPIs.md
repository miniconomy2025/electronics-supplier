# External API Configuration

## Overview

External API endpoints are configured through the ASP.NET Core configuration system, supporting both JSON configuration files and environment variables.

## Configuration Sources (in order of precedence)

1. **Environment Variables** (highest precedence)
2. **appsettings.{Environment}.json** (e.g., appsettings.Development.json)
3. **appsettings.json** (lowest precedence)

## Configuration Structure

### JSON Configuration

```json
{
  "ExternalApis": {
    "CommercialBank": "https://commercial-bank-api.subspace.site/api",
    "BulkLogistics": "https://team7-todo.xyz/api",
    "THOH": "https://ec2-13-244-65-62.af-south-1.compute.amazonaws.com",
    "Recycler": "https://api.recycler.susnet.co.za"
  }
}
```

### Environment Variables

You can override any configuration using environment variables with the pattern:

```bash
# General pattern: SectionName__PropertyName
ExternalApis__CommercialBank=https://your-commercial-bank-api.com
ExternalApis__BulkLogistics=https://your-bulk-logistics-api.com
ExternalApis__THOH=https://your-thoh-api.com
ExternalApis__Recycler=https://your-recycler-api.com

# Optional: Override the client ID sent in headers
ExternalApis__ClientId=your-custom-client-id
```

### Docker Environment Variables

```bash
docker run -e ExternalApis__CommercialBank=https://prod-bank-api.com \
           -e ExternalApis__BulkLogistics=https://prod-logistics.com \
           your-app-image
```

### Azure App Service Configuration

In Azure Portal → Configuration → Application Settings:

- Name: `ExternalApis__CommercialBank`
- Value: `https://prod-bank-api.com`

## Validation

The application validates that all required API endpoints are configured on startup. If any are missing, it will throw an `InvalidOperationException` with a descriptive error message.

## Security Notes

- Never commit production URLs to source control if they contain sensitive information
- Use environment variables for production deployments
- Consider using Azure Key Vault or similar secret management for sensitive URLs
