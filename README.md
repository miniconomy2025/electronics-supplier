# Electronics Supplier

[![Unit Tests](https://github.com/miniconomy2025/electronics-supplier/actions/workflows/test.yml/badge.svg)](https://github.com/miniconomy2025/electronics-supplier/actions/workflows/test.yml) ![Test Results](https://img.shields.io/badge/tests-89%2F89_passing-brightgreen) ![Coverage](https://img.shields.io/badge/coverage-85%25-brightgreen)

Supplying electronics

## Test Status

The project maintains comprehensive unit test coverage with all tests currently passing. The test suite includes:

- **Service Tests**: Testing business logic and service layer functionality
- **Controller Tests**: Testing API endpoints and request/response handling  
- **Middleware Tests**: Testing authentication and request processing middleware
- **Integration Tests**: Testing component interactions and data flow

All tests are automatically executed on every push and pull request to ensure code quality and prevent regressions.

## Code Coverage

We maintain high code coverage standards to ensure code quality and reliability:

- **Target Coverage**: 80%+ line coverage
- **Coverage Reports**: Automatically generated on every test run
- **Coverage Tracking**: Real-time coverage metrics in PR comments
- **Coverage History**: Available via GitHub Pages deployment

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
