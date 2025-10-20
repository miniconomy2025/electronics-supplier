#!/bin/bash

# Production startup script for Electronics Supplier API
# This script configures the application for production environment

set -e

APP_DIR="/home/ubuntu/es/esAPI"
LOG_FILE="/home/ubuntu/startup.log"

# Get RDS endpoint from environment or use default
RDS_ENDPOINT=${RDS_ENDPOINT:-"your-rds-instance.cluster-xyz.af-south-1.rds.amazonaws.com"}
RDS_USERNAME=${RDS_USERNAME:-"postgres"}
RDS_PASSWORD=${RDS_PASSWORD:-"your-db-password"}
RDS_DATABASE=${RDS_DATABASE:-"es_db"}

log() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $1" | tee -a "$LOG_FILE"
}

# Check if we're in the correct directory
if [ ! -f "$APP_DIR/esAPI.csproj" ]; then
    log "ERROR: Not in the correct directory. Expected to find esAPI.csproj"
    exit 1
fi

cd "$APP_DIR"

log "Starting Electronics Supplier API in production mode..."
log "Working directory: $(pwd)"
log "Environment: Production"

# Set environment variables to match current EC2 setup
export ASPNETCORE_ENVIRONMENT=Development  # Matches current EC2 setup
export ASPNETCORE_URLS="http://0.0.0.0:5062"

# Database connection is configured in appsettings.json (copied from appsettings.Production.json)
log "Using database connection from appsettings.json"
log "RDS endpoint: es-db.cnhrl5wk3uki.af-south-1.rds.amazonaws.com"

log "Configuration:"
log "- Environment: $ASPNETCORE_ENVIRONMENT"
log "- URLs: $ASPNETCORE_URLS"
log "- Database: Configured in appsettings.json"
log "- External APIs: Configured in appsettings.json"

# Run the application
log "Starting dotnet application..."
exec dotnet run --configuration Release --no-build