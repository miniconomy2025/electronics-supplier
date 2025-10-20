# Electronics Supplier

[![Unit Tests](https://github.com/miniconomy2025/electronics-supplier/actions/workflows/test.yml/badge.svg)](https://github.com/miniconomy2025/electronics-supplier/actions/workflows/test.yml) [![Docker Build](https://github.com/miniconomy2025/electronics-supplier/actions/workflows/docker.yml/badge.svg)](https://github.com/miniconomy2025/electronics-supplier/actions/workflows/docker.yml) [![Integration Tests](https://github.com/miniconomy2025/electronics-supplier/actions/workflows/integration.yml/badge.svg)](https://github.com/miniconomy2025/electronics-supplier/actions/workflows/integration.yml) [![Deploy](https://github.com/miniconomy2025/electronics-supplier/actions/workflows/deploy.yml/badge.svg)](https://github.com/miniconomy2025/electronics-supplier/actions/workflows/deploy.yml)

![Test Results](https://img.shields.io/badge/tests-89%2F89_passing-brightgreen) ![Coverage](https://img.shields.io/badge/coverage-85%25-brightgreen) ![Docker](https://img.shields.io/badge/docker-ready-blue) ![Deployment](https://img.shields.io/badge/deployment-automated-green)

Modern electronics supplier platform with streamlined CI/CD pipeline, Docker containerization, and automated deployment.

## Test Status

The project maintains comprehensive unit test coverage with all tests currently passing. The test suite includes:

- **Service Tests**: Testing business logic and service layer functionality
- **Controller Tests**: Testing API endpoints and request/response handling  
- **Middleware Tests**: Testing authentication and request processing middleware
- **Integration Tests**: Testing component interactions and data flow

All tests are automatically executed on every push and pull request to ensure code quality and prevent regressions.

## CI/CD Pipeline

The project features a focused CI/CD pipeline with essential stages:

- **🧪 Testing**: Unit tests, integration tests, and code coverage
- **🐳 Docker**: Automated container builds and registry publishing  
- **🚀 Deployment**: Automated EC2 deployment with rollback capabilities

## Deployment

The application supports automated deployment to AWS EC2 with zero-downtime updates:

### Production Deployment

1. **Automated Pipeline**: Triggered on pushes to `main` branch
2. **Pre-deployment Validation**: Tests, builds, and health checks
3. **SSH Deployment**: Secure connection to EC2 instance
4. **Application Management**: tmux session handling and process management
5. **Health Monitoring**: Automatic rollback on deployment failures

### Manual Deployment

For manual deployments or troubleshooting:

```bash
# On EC2 instance
./deploy.sh

# Or use the production startup script
./start-production.sh
```

### Configuration

**GitHub Secrets Required:**

- `EC2_HOST`: EC2 instance public IP or hostname
- `EC2_PRIVATE_KEY`: EC2 instance private key (PEM format)
- `AWS_ACCESS_KEY_ID`: AWS access key for additional services (optional)
- `AWS_SECRET_ACCESS_KEY`: AWS secret key for additional services (optional)

**Note**: Database connection and external API configurations are now embedded in `appsettings.Production.json` and don't require separate secrets.

## Test Coverage

### Running Coverage Locally

**Linux/macOS:**

```bash
./run-coverage.sh
```

**Windows:**

```powershell
./run-coverage.ps1
```

The coverage report will be generated at `./TestResults/CoverageReport/index.html` and automatically opened in your browser.

### Coverage Configuration

- **Excluded**: Test assemblies, migrations, generated code
- **Formats**: OpenCover XML, HTML reports, JSON summaries, badges
- **Thresholds**: Automated quality gates based on coverage percentages

## Members

- Tevlen Naidoo
- Kyle Wilkins
- Shailyn Ramsamy Moodley
- Samkele Munyu
- Rorisang Shadung
