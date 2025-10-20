#!/bin/bash

echo "🧪 Running tests with code coverage..."

# Clean previous results
rm -rf TestResults

# Run tests with coverage
dotnet test --configuration Release \
  --logger trx --logger "console;verbosity=normal" \
  --collect:"XPlat Code Coverage" \
  --results-directory ./TestResults \
  --filter "Category!=Integration" \
  --settings CodeCoverage.runsettings

# Install ReportGenerator if not present
if ! command -v reportgenerator &> /dev/null; then
    echo "📊 Installing ReportGenerator..."
    dotnet tool install -g dotnet-reportgenerator-globaltool
fi

# Generate coverage report
echo "📊 Generating coverage report..."
reportgenerator \
  -reports:"./TestResults/**/coverage.opencover.xml" \
  -targetdir:"./TestResults/CoverageReport" \
  -reporttypes:"Html;JsonSummary;Badges;TextSummary;Xml" \
  -verbosity:Warning

# Display summary
if [ -f "./TestResults/CoverageReport/Summary.txt" ]; then
    echo ""
    echo "📈 Coverage Summary:"
    cat "./TestResults/CoverageReport/Summary.txt"
fi

# Extract and display key metrics
if [ -f "./TestResults/CoverageReport/Summary.json" ]; then
    LINE_COVERAGE=$(jq -r '.summary.linecoverage' "./TestResults/CoverageReport/Summary.json")
    BRANCH_COVERAGE=$(jq -r '.summary.branchcoverage' "./TestResults/CoverageReport/Summary.json")
    
    echo ""
    echo "🎯 Key Metrics:"
    echo "   Line Coverage: $LINE_COVERAGE"
    echo "   Branch Coverage: $BRANCH_COVERAGE"
fi

echo ""
echo "✅ Coverage report generated at: ./TestResults/CoverageReport/index.html"
echo "🌐 Open in browser: file://$(pwd)/TestResults/CoverageReport/index.html"