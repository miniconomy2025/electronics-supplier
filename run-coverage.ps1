# PowerShell script for running code coverage on Windows

Write-Host "🧪 Running tests with code coverage..." -ForegroundColor Cyan

# Clean previous results
if (Test-Path "TestResults") {
    Remove-Item -Recurse -Force "TestResults"
}

# Run tests with coverage
dotnet test --configuration Release `
  --logger trx --logger "console;verbosity=normal" `
  --collect:"XPlat Code Coverage" `
  --results-directory ./TestResults `
  --filter "Category!=Integration" `
  --settings CodeCoverage.runsettings

# Install ReportGenerator if not present
if (-not (Get-Command reportgenerator -ErrorAction SilentlyContinue)) {
    Write-Host "📊 Installing ReportGenerator..." -ForegroundColor Yellow
    dotnet tool install -g dotnet-reportgenerator-globaltool
}

# Generate coverage report
Write-Host "📊 Generating coverage report..." -ForegroundColor Cyan
reportgenerator `
  -reports:"./TestResults/**/coverage.opencover.xml" `
  -targetdir:"./TestResults/CoverageReport" `
  -reporttypes:"Html;JsonSummary;Badges;TextSummary;Xml" `
  -verbosity:Warning

# Display summary
if (Test-Path "./TestResults/CoverageReport/Summary.txt") {
    Write-Host ""
    Write-Host "📈 Coverage Summary:" -ForegroundColor Green
    Get-Content "./TestResults/CoverageReport/Summary.txt"
}

# Extract and display key metrics
if (Test-Path "./TestResults/CoverageReport/Summary.json") {
    $summary = Get-Content "./TestResults/CoverageReport/Summary.json" | ConvertFrom-Json
    $lineCoverage = $summary.summary.linecoverage
    $branchCoverage = $summary.summary.branchcoverage
    
    Write-Host ""
    Write-Host "🎯 Key Metrics:" -ForegroundColor Green
    Write-Host "   Line Coverage: $lineCoverage" -ForegroundColor White
    Write-Host "   Branch Coverage: $branchCoverage" -ForegroundColor White
}

Write-Host ""
Write-Host "✅ Coverage report generated at: ./TestResults/CoverageReport/index.html" -ForegroundColor Green

# Try to open the report in the default browser
$reportPath = Resolve-Path "./TestResults/CoverageReport/index.html"
Write-Host "🌐 Opening coverage report in browser..." -ForegroundColor Cyan
Start-Process $reportPath