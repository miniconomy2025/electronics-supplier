@echo off
REM Production startup script for Electronics Supplier API (Windows)
REM This script configures the application for production environment

SET APP_DIR=C:\path\to\esAPI
SET LOG_FILE=C:\path\to\startup.log

REM Get RDS endpoint from environment or use default
IF "%RDS_ENDPOINT%"=="" SET RDS_ENDPOINT=your-rds-instance.cluster-xyz.af-south-1.rds.amazonaws.com
IF "%RDS_USERNAME%"=="" SET RDS_USERNAME=postgres
IF "%RDS_PASSWORD%"=="" SET RDS_PASSWORD=your-db-password
IF "%RDS_DATABASE%"=="" SET RDS_DATABASE=es_db

echo [%date% %time%] Starting Electronics Supplier API in production mode... >> %LOG_FILE%
echo [%date% %time%] Working directory: %CD% >> %LOG_FILE%
echo [%date% %time%] Environment: Production >> %LOG_FILE%

REM Change to application directory
cd /d %APP_DIR%

REM Check if we're in the correct directory
IF NOT EXIST esAPI.csproj (
    echo [%date% %time%] ERROR: Not in the correct directory. Expected to find esAPI.csproj >> %LOG_FILE%
    exit /b 1
)

REM Set production environment variables
SET ASPNETCORE_ENVIRONMENT=Production
SET ASPNETCORE_URLS=http://0.0.0.0:5062

REM Set database connection string for production
IF NOT "%RDS_ENDPOINT%"=="your-rds-instance.cluster-xyz.af-south-1.rds.amazonaws.com" (
    SET ConnectionStrings__DefaultConnection=Host=%RDS_ENDPOINT%;Port=5432;Database=%RDS_DATABASE%;Username=%RDS_USERNAME%;Password=%RDS_PASSWORD%
    echo [%date% %time%] Using RDS database: %RDS_ENDPOINT% >> %LOG_FILE%
) ELSE (
    echo [%date% %time%] WARNING: Using default database configuration. Set RDS_ENDPOINT for production. >> %LOG_FILE%
)

echo [%date% %time%] Configuration: >> %LOG_FILE%
echo [%date% %time%] - Environment: %ASPNETCORE_ENVIRONMENT% >> %LOG_FILE%
echo [%date% %time%] - URLs: %ASPNETCORE_URLS% >> %LOG_FILE%

REM Run the application
echo [%date% %time%] Starting dotnet application... >> %LOG_FILE%
dotnet run --configuration Release --no-build