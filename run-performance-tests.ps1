# Performance testing script for Electronics Supplier API (PowerShell)
# Usage: .\run-performance-tests.ps1 [TestType] [BaseUrl]

param(
    [Parameter(Position=0)]
    [ValidateSet("smoke", "load", "stress", "spike", "endurance", "all", "help")]
    [string]$TestType = "smoke",
    
    [Parameter(Position=1)]
    [string]$BaseUrl = "http://localhost:5000"
)

# Configuration
$ResultsDir = "./performance-results"

# Function to print colored output
function Write-Status {
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor Blue
}

function Write-Success {
    param([string]$Message)
    Write-Host "[SUCCESS] $Message" -ForegroundColor Green
}

function Write-Warning {
    param([string]$Message)
    Write-Host "[WARNING] $Message" -ForegroundColor Yellow
}

function Write-Error {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor Red
}

# Check if k6 is installed
function Test-K6Installation {
    if (-not (Get-Command k6 -ErrorAction SilentlyContinue)) {
        Write-Error "k6 is not installed. Please install k6 first:"
        Write-Host ""
        Write-Host "Windows (Chocolatey): choco install k6" -ForegroundColor White
        Write-Host "Windows (Manual): Download from https://github.com/grafana/k6/releases" -ForegroundColor White
        exit 1
    }
    Write-Success "k6 is installed"
}

# Check if API is running and if it's AWS-hosted
function Test-ApiAvailability {
    Write-Status "Checking if API is running at $BaseUrl..."
    
    # Check if URL suggests AWS hosting
    $isAwsHosted = $BaseUrl -match "(\.amazonaws\.com|\.aws\.|ec2-|elasticbeanstalk)" -or 
                   $BaseUrl -match "(\.compute\.amazonaws\.com)"
    
    if ($isAwsHosted -and $TestType -notin @("smoke", "help")) {
        Write-Warning "🚨 AWS-HOSTED ENDPOINT DETECTED!"
        Write-Warning "Target: $BaseUrl"
        Write-Warning ""
        Write-Warning "CAUTION: Load testing AWS-hosted applications can:"
        Write-Warning "- Trigger DDoS protection mechanisms"
        Write-Warning "- Result in IP blacklisting"
        Write-Warning "- Generate unexpected AWS charges"
        Write-Warning "- Violate AWS Acceptable Use Policy"
        Write-Warning ""
        Write-Warning "RECOMMENDED:"
        Write-Warning "- Use 'smoke' test only for AWS environments"
        Write-Warning "- Get AWS Support approval for load testing"
        Write-Warning "- Use dedicated staging environment"
        Write-Warning "- Consider AWS Load Testing solution instead"
        Write-Warning ""
        
        $response = Read-Host "Continue anyway? (yes/no)"
        if ($response -ne "yes") {
            Write-Host "Aborting for safety. Use 'smoke' test or local environment." -ForegroundColor Red
            exit 1
        }
    }
    
    for ($i = 1; $i -le 10; $i++) {
        try {
            $response = Invoke-WebRequest -Uri "$BaseUrl/health" -Method Get -TimeoutSec 5 -ErrorAction Stop
            if ($response.StatusCode -eq 200) {
                Write-Success "API is running and responding"
                
                if ($isAwsHosted) {
                    Write-Warning "⚠️  Remember: Monitor CloudWatch for any throttling during test"
                }
                return
            }
        }
        catch {
            # API not ready yet
        }
        
        if ($i -eq 10) {
            Write-Error "API is not responding at $BaseUrl"
            Write-Warning "Please start your API first:"
            Write-Warning "  cd esAPI; dotnet run"
            exit 1
        }
        
        Write-Status "Waiting for API... (attempt $i/10)"
        Start-Sleep -Seconds 2
    }
}

# Create results directory
function Initialize-ResultsDirectory {
    if (-not (Test-Path $ResultsDir)) {
        New-Item -ItemType Directory -Path $ResultsDir | Out-Null
    }
    Write-Status "Results will be saved to: $ResultsDir"
}

# Run specific test
function Invoke-PerformanceTest {
    param([string]$TestName)
    
    $testFile = "performance-tests/$TestName.js"
    
    if (-not (Test-Path $testFile)) {
        Write-Error "Test file not found: $testFile"
        exit 1
    }
    
    Write-Status "Running $TestName..."
    Write-Status "Target: $BaseUrl"
    
    # Change to performance-tests directory
    Push-Location "performance-tests"
    
    try {
        # Run the test with results output
        $resultsFile = "../$ResultsDir/$TestName-results.json"
        $k6Args = @(
            "run"
            "--out", "json=$resultsFile"
            "-e", "BASE_URL=$BaseUrl"
            "$TestName.js"
        )
        
        $process = Start-Process -FilePath "k6" -ArgumentList $k6Args -Wait -PassThru -NoNewWindow
        
        if ($process.ExitCode -eq 0) {
            Write-Success "$TestName completed successfully"
            
            # Move summary file if it exists
            $summaryFile = "$TestName-summary.json"
            if (Test-Path $summaryFile) {
                Move-Item $summaryFile "../$ResultsDir/" -Force
            }
            
            return $true
        }
        else {
            Write-Error "$TestName failed with exit code $($process.ExitCode)"
            return $false
        }
    }
    finally {
        Pop-Location
    }
}

# Display results summary
function Show-TestSummary {
    param([string]$TestName)
    
    $resultsFile = "$ResultsDir/$TestName-results.json"
    
    if (Test-Path $resultsFile) {
        Write-Status "📊 $TestName Summary:"
        
        try {
            # Try to parse JSON and extract metrics
            $results = Get-Content $resultsFile | ConvertFrom-Json
            $metrics = $results.metrics
            
            if ($metrics.http_req_duration.values.avg) {
                $avgResponse = [math]::Round($metrics.http_req_duration.values.avg, 2)
                Write-Host "  Average Response Time: ${avgResponse}ms" -ForegroundColor White
            }
            
            if ($metrics.http_req_duration.values.'p(95)') {
                $p95Response = [math]::Round($metrics.http_req_duration.values.'p(95)', 2)
                Write-Host "  95th Percentile: ${p95Response}ms" -ForegroundColor White
            }
            
            if ($metrics.http_req_failed.values.rate) {
                $errorRate = [math]::Round($metrics.http_req_failed.values.rate * 100, 2)
                Write-Host "  Error Rate: ${errorRate}%" -ForegroundColor White
            }
            
            if ($metrics.http_reqs.values.count) {
                $totalRequests = $metrics.http_reqs.values.count
                Write-Host "  Total Requests: $totalRequests" -ForegroundColor White
            }
        }
        catch {
            Write-Host "  Results available in: $resultsFile" -ForegroundColor White
        }
        
        Write-Host ""
    }
}

# Print usage
function Show-Usage {
    Write-Host "Usage: .\run-performance-tests.ps1 [TestType] [BaseUrl]" -ForegroundColor White
    Write-Host ""
    Write-Host "Test Types:" -ForegroundColor Yellow
    Write-Host "  smoke      - Quick validation (5 users, 30s)" -ForegroundColor White
    Write-Host "  load       - Normal load testing (0→10→0 users)" -ForegroundColor White
    Write-Host "  stress     - Stress testing (0→150 users)" -ForegroundColor White
    Write-Host "  spike      - Spike testing (sudden load spikes)" -ForegroundColor White
    Write-Host "  endurance  - Long-running test (20 users, 14min)" -ForegroundColor White
    Write-Host "  all        - Run all tests sequentially" -ForegroundColor White
    Write-Host ""
    Write-Host "Examples:" -ForegroundColor Yellow
    Write-Host "  .\run-performance-tests.ps1 smoke" -ForegroundColor White
    Write-Host "  .\run-performance-tests.ps1 load http://localhost:5000" -ForegroundColor White
    Write-Host "  .\run-performance-tests.ps1 all https://your-api.com" -ForegroundColor White
}

# Main execution
function Main {
    Write-Host "🚀 Electronics Supplier Performance Testing" -ForegroundColor Cyan
    Write-Host "===========================================" -ForegroundColor Cyan
    
    # Handle help
    if ($TestType -eq "help") {
        Show-Usage
        exit 0
    }
    
    # Setup
    Test-K6Installation
    Initialize-ResultsDirectory
    Test-ApiAvailability
    
    Write-Status "Starting performance tests..."
    Write-Status "Test type: $TestType"
    Write-Status "Base URL: $BaseUrl"
    Write-Host ""
    
    # Run tests
    if ($TestType -eq "all") {
        # Run all tests
        $tests = @("smoke-test", "load-test", "stress-test", "spike-test", "endurance-test")
        $failedTests = @()
        
        foreach ($test in $tests) {
            Write-Status "🔄 Running $test..."
            if (Invoke-PerformanceTest $test) {
                Show-TestSummary $test
            }
            else {
                $failedTests += $test
            }
            
            # Add delay between tests (except for the last one)
            if ($test -ne "endurance-test") {
                Write-Status "Waiting 30 seconds before next test..."
                Start-Sleep -Seconds 30
            }
        }
        
        # Summary
        Write-Host "🏁 All tests completed!" -ForegroundColor Cyan
        if ($failedTests.Count -eq 0) {
            Write-Success "All tests passed!"
        }
        else {
            Write-Warning "Some tests failed: $($failedTests -join ', ')"
        }
    }
    else {
        # Run single test
        $testFile = "$TestType-test"
        if (Invoke-PerformanceTest $testFile) {
            Show-TestSummary $testFile
            Write-Success "Test completed successfully!"
        }
        else {
            Write-Error "Test failed!"
            exit 1
        }
    }
    
    # Final information
    Write-Host ""
    Write-Status "📁 Results saved to: $ResultsDir"
    Write-Status "📊 View detailed results:"
    Write-Host "  - JSON results: $ResultsDir/$TestType-test-results.json" -ForegroundColor White
    Write-Host "  - Summary: $ResultsDir/$TestType-test-summary.json" -ForegroundColor White
    Write-Host ""
    Write-Success "Performance testing completed! 🎉"
}

# Run main function
Main