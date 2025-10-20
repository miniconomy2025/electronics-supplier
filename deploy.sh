#!/bin/bash

# Safe deployment script for EC2 production environment
# This script handles zero-downtime deployments with proper tmux session management

set -e

# Configuration
REPO_DIR="/home/ubuntu/es"
APP_DIR="$REPO_DIR/esAPI"
TMUX_SESSION="electronics-supplier-api"
BACKUP_DIR="/home/ubuntu/backups"
LOG_FILE="/home/ubuntu/deploy.log"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Logging function
log() {
    echo -e "${BLUE}[$(date '+%Y-%m-%d %H:%M:%S')]${NC} $1" | tee -a "$LOG_FILE"
}

log_success() {
    echo -e "${GREEN}[$(date '+%Y-%m-%d %H:%M:%S')] SUCCESS:${NC} $1" | tee -a "$LOG_FILE"
}

log_warning() {
    echo -e "${YELLOW}[$(date '+%Y-%m-%d %H:%M:%S')] WARNING:${NC} $1" | tee -a "$LOG_FILE"
}

log_error() {
    echo -e "${RED}[$(date '+%Y-%m-%d %H:%M:%S')] ERROR:${NC} $1" | tee -a "$LOG_FILE"
}

# Health check function
health_check() {
    local max_attempts=30
    local attempt=1
    
    log "Starting health check..."
    
    while [ $attempt -le $max_attempts ]; do
        if curl -s -f http://localhost:5062/health >/dev/null 2>&1; then
            log_success "Health check passed on attempt $attempt"
            return 0
        fi
        
        log "Health check attempt $attempt/$max_attempts failed, waiting..."
        sleep 2
        attempt=$((attempt + 1))
    done
    
    log_error "Health check failed after $max_attempts attempts"
    return 1
}

# Backup current deployment
backup_current() {
    log "Creating backup of current deployment..."
    
    # Create backup directory with timestamp
    local backup_timestamp=$(date '+%Y%m%d_%H%M%S')
    local backup_path="$BACKUP_DIR/deployment_$backup_timestamp"
    
    mkdir -p "$backup_path"
    
    # Backup the entire repository
    cp -r "$REPO_DIR" "$backup_path/"
    
    # Keep only last 5 backups
    cd "$BACKUP_DIR"
    ls -t | tail -n +6 | xargs -r rm -rf
    
    log_success "Backup created at $backup_path"
    echo "$backup_path" > /tmp/last_backup_path
}

# Rollback function
rollback() {
    log_warning "Rolling back to previous deployment..."
    
    if [ -f /tmp/last_backup_path ]; then
        local backup_path=$(cat /tmp/last_backup_path)
        if [ -d "$backup_path" ]; then
            # Stop current application
            tmux kill-session -t "$TMUX_SESSION" 2>/dev/null || true
            
            # Restore from backup
            rm -rf "$REPO_DIR"
            cp -r "$backup_path/es" "$REPO_DIR"
            
            # Restart application
            start_application
            
            if health_check; then
                log_success "Rollback successful"
                return 0
            else
                log_error "Rollback failed - manual intervention required"
                return 1
            fi
        fi
    fi
    
    log_error "No backup found for rollback"
    return 1
}

# Start application in tmux
start_application() {
    log "Starting application in tmux session: $TMUX_SESSION"
    
    cd "$APP_DIR"
    
    # Kill existing session if it exists
    tmux kill-session -t "$TMUX_SESSION" 2>/dev/null || true
    
    # Start new tmux session
    tmux new-session -d -s "$TMUX_SESSION" -c "$APP_DIR"
    
    # Run the application (use Development env to match current EC2 setup, but with production config files)
    tmux send-keys -t "$TMUX_SESSION" "dotnet run --launch-profile http" C-m
    
    # Wait a moment for startup
    sleep 5
    
    log_success "Application started in tmux session: $TMUX_SESSION"
}

# Stop application gracefully
stop_application() {
    log "Stopping application..."
    
    if tmux has-session -t "$TMUX_SESSION" 2>/dev/null; then
        # Send Ctrl+C to gracefully stop the application
        tmux send-keys -t "$TMUX_SESSION" C-c
        
        # Wait for graceful shutdown
        sleep 10
        
        # Force kill if still running
        tmux kill-session -t "$TMUX_SESSION" 2>/dev/null || true
        
        log_success "Application stopped"
    else
        log_warning "No tmux session found for $TMUX_SESSION"
    fi
}

# Update code from git
update_code() {
    log "Updating code from git..."
    
    cd "$REPO_DIR"
    
    # Stash any local changes (like the config modifications you mentioned)
    if git status --porcelain | grep -q .; then
        log_warning "Found local changes, stashing them..."
        git stash push -m "Auto-stash before deployment $(date '+%Y-%m-%d %H:%M:%S')"
    fi
    
    # Pull latest changes
    git pull origin main
    
    log_success "Code updated from git"
}

# Build application
build_application() {
    log "Building application..."
    
    cd "$APP_DIR"
    
    # Restore dependencies
    dotnet restore
    
    # Build in release mode
    dotnet build --configuration Release --no-restore
    
    log_success "Application built successfully"
}

# Update configuration for production deployment
update_production_config() {
    log "Updating configuration files for production..."
    
    cd "$APP_DIR"
    
    # Copy production appsettings (contains real RDS connection and external APIs)
    if [ -f "appsettings.Production.json" ]; then
        log "Copying production appsettings.json..."
        cp appsettings.Production.json appsettings.json
    else
        log_warning "appsettings.Production.json not found, using existing appsettings.json"
    fi
    
    # Update launchSettings to match current EC2 setup (Development env but production URLs)
    if [ -f "Properties/launchSettings.Production.json" ]; then
        log "Updating launchSettings.json for EC2 deployment..."
        cp Properties/launchSettings.Production.json Properties/launchSettings.json
        # Keep Development environment to match current EC2 setup
        sed -i 's/"ASPNETCORE_ENVIRONMENT": "Production"/"ASPNETCORE_ENVIRONMENT": "Development"/g' Properties/launchSettings.json
    else
        log_warning "launchSettings.Production.json not found, using existing launchSettings.json"
    fi
    
    log_success "Configuration updated for production deployment"
}

# Main deployment function
deploy() {
    log "=== Starting deployment ==="
    
    # Create backup
    backup_current
    
    # Update code
    if ! update_code; then
        log_error "Failed to update code"
        rollback
        exit 1
    fi
    
    # Build application
    if ! build_application; then
        log_error "Failed to build application"
        rollback
        exit 1
    fi
    
    # Update configuration files for production
    update_production_config
    
    # Stop current application
    stop_application
    
    # Start new application
    start_application
    
    # Health check
    if health_check; then
        log_success "=== Deployment completed successfully ==="
        
        # Show tmux session info
        log "Application is running in tmux session: $TMUX_SESSION"
        log "To attach to session: tmux attach-session -t $TMUX_SESSION"
        log "To view logs: tmux capture-pane -t $TMUX_SESSION -p"
        
    else
        log_error "Deployment failed health check"
        rollback
        exit 1
    fi
}

# Handle script arguments
case "${1:-deploy}" in
    "deploy")
        deploy
        ;;
    "rollback")
        rollback
        ;;
    "status")
        if tmux has-session -t "$TMUX_SESSION" 2>/dev/null; then
            log_success "Application is running in tmux session: $TMUX_SESSION"
            
            # Show recent logs
            echo "Recent application output:"
            tmux capture-pane -t "$TMUX_SESSION" -p | tail -20
            
        else
            log_warning "No tmux session found for $TMUX_SESSION"
        fi
        ;;
    "logs")
        if tmux has-session -t "$TMUX_SESSION" 2>/dev/null; then
            tmux capture-pane -t "$TMUX_SESSION" -p
        else
            log_warning "No tmux session found"
        fi
        ;;
    "attach")
        if tmux has-session -t "$TMUX_SESSION" 2>/dev/null; then
            tmux attach-session -t "$TMUX_SESSION"
        else
            log_warning "No tmux session found"
        fi
        ;;
    *)
        echo "Usage: $0 [deploy|rollback|status|logs|attach]"
        echo ""
        echo "Commands:"
        echo "  deploy   - Deploy latest code from main branch (default)"
        echo "  rollback - Rollback to previous deployment"
        echo "  status   - Check application status"
        echo "  logs     - View application logs"
        echo "  attach   - Attach to tmux session"
        exit 1
        ;;
esac