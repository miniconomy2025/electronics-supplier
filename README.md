# Electronics Supplier

[![Unit Tests](https://github.com/miniconomy2025/electronics-supplier/actions/workflows/test.yml/badge.svg)](https://github.com/miniconomy2025/electronics-supplier/actions/workflows/test.yml) [![Docker Build](https://github.com/miniconomy2025/electronics-supplier/actions/workflows/docker.yml/badge.svg)](https://github.com/miniconomy2025/electronics-supplier/actions/workflows/docker.yml) [![Integration Tests](https://github.com/miniconomy2025/electronics-supplier/actions/workflows/integration.yml/badge.svg)](https://github.com/miniconomy2025/electronics-supplier/actions/workflows/integration.yml) [![Infrastructure Tests](https://github.com/miniconomy2025/electronics-supplier/actions/workflows/infrastructure.yml/badge.svg)](https://github.com/miniconomy2025/electronics-supplier/actions/workflows/infrastructure.yml) [![Performance Tests](https://github.com/miniconomy2025/electronics-supplier/actions/workflows/performance.yml/badge.svg)](https://github.com/miniconomy2025/electronics-supplier/actions/workflows/performance.yml) [![Deploy](https://github.com/miniconomy2025/electronics-supplier/actions/workflows/deploy.yml/badge.svg)](https://github.com/miniconomy2025/electronics-supplier/actions/workflows/deploy.yml)

![Test Results](https://img.shields.io/badge/tests-89%2F89_passing-brightgreen) ![Coverage](https://img.shields.io/badge/coverage-85%25-brightgreen) ![Docker](https://img.shields.io/badge/docker-ready-blue) ![Deployment](https://img.shields.io/badge/deployment-automated-green)

Modern electronics supplier platform with comprehensive CI/CD pipeline, Docker containerization, and automated AWS deployment.

## Test Status

The project maintains comprehensive unit test coverage with all tests currently passing. The test suite includes:

- **Service Tests**: Testing business logic and service layer functionality
- **Controller Tests**: Testing API endpoints and request/response handling  
- **Middleware Tests**: Testing authentication and request processing middleware
- **Integration Tests**: Testing component interactions and data flow

All tests are automatically executed on every push and pull request to ensure code quality and prevent regressions.

## CI/CD Pipeline

The project features a comprehensive CI/CD pipeline with multiple stages:

- **🧪 Testing**: Unit tests, integration tests, and code coverage
- **🐳 Docker**: Automated container builds and registry publishing
- **🏗️ Infrastructure**: Terraform validation and security scanning
- **⚡ Performance**: AWS-safe performance testing with k6
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

## Performance Testing

The project includes comprehensive performance testing using k6 with **AWS-safe defaults** to prevent accidental blacklisting or rate limiting:

- **Smoke Tests**: Quick validation (5 users, 30s) - **AWS-safe**, runs on every PR
- **Load Tests**: Normal traffic simulation - **LOCAL ONLY** or with explicit approval
- **Stress Tests**: Breaking point analysis - **LOCAL ONLY**
- **Spike Tests**: Traffic surge simulation - **LOCAL ONLY**
- **Endurance Tests**: Memory leak detection - **LOCAL ONLY**

### ⚠️ AWS Safety Notice

**IMPORTANT**: Load testing against AWS-hosted applications requires:

- AWS Support approval for anything beyond smoke tests
- Dedicated staging environments
- Monitoring for rate limiting and throttling
- Cost monitoring and budget alerts

See `performance-tests/AWS-SAFETY-GUIDE.md` for complete AWS testing guidelines.

### Running Performance Tests Locally

**Prerequisites**: Install [k6](https://k6.io/docs/getting-started/installation/)

**Linux/macOS:**

```bash
# AWS-safe smoke test
./run-performance-tests.sh smoke

# Local development testing
./run-performance-tests.sh load http://localhost:5000
```

**Windows:**

```powershell
# AWS-safe smoke test  
./run-performance-tests.ps1 smoke

# Local development testing
./run-performance-tests.ps1 load http://localhost:5000
```

### AWS-Safe Performance Metrics

| Test Type | Users | Duration | AWS Safe | Purpose |
|-----------|-------|----------|----------|---------|
| Smoke     | 5     | 30s      | ✅ Yes   | Basic validation |
| Load      | 10    | 2min     | ⚠️ Local only | Baseline performance |
| Stress    | 150   | 12min    | ❌ Never | Breaking point |
| Spike     | 300   | 3min     | ❌ Never | Resilience testing |

**Pipeline Integration**: Only smoke tests run automatically against AWS. Load tests require manual approval and local environments.

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
