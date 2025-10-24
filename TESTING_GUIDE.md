# MongoDB & New Relic Testing Script

## Quick Test Commands

### 1. Test MongoDB Connection
```bash
# Test MongoDB directly
mongosh mongodb://localhost:27017/AutoFixer --eval "db.runCommand('ping')"

# OR using Docker
docker exec -it org-licensing-mongo-v5 mongosh --eval "db.runCommand('ping')"
```

### 2. Test AutoFixer Health
```bash
# Start application
dotnet run --project "D:\AutoFixer\AutoFixer\AutoFixer.csproj" --urls "http://localhost:5000"

# In another terminal, test health
curl http://localhost:5000/health
# OR
Invoke-WebRequest -Uri "http://localhost:5000/health" -UseBasicParsing
```

### 3. Test API Endpoints
```bash
# Test root endpoint
curl http://localhost:5000/

# Test patterns endpoint
curl http://localhost:5000/api/patterns

# Test alerts endpoint  
curl http://localhost:5000/api/alerts

# Test dashboard endpoint
curl http://localhost:5000/api/dashboard/overview
```

### 4. Initialize MongoDB with Sample Data
```bash
# Copy the mongo-init.js file and run:
mongosh AutoFixer < mongo-init.js

# OR connect to existing container:
docker exec -i org-licensing-mongo-v5 mongosh AutoFixer < mongo-init.js
```

### 5. Test New Relic Integration
First configure New Relic in appsettings.json:
```json
{
  "NewRelic": {
    "AccountId": "YOUR_ACCOUNT_ID",
    "ApiKey": "YOUR_API_KEY", 
    "ApplicationName": "AutoFixer"
  }
}
```

Then test:
```bash
# Trigger pattern detection (which uses New Relic)
curl -X POST "http://localhost:5000/api/patterns/detect" \
  -H "Content-Type: application/json" \
  -d '{
    "logEntries": [
      "2024-01-15 10:30:00 ERROR: OutOfMemoryException in service",
      "2024-01-15 10:35:00 ERROR: SQL injection attempt detected"
    ],
    "source": "TestAPI"
  }'
```

## Environment Variables for Production

```bash
# MongoDB
export MONGODB__CONNECTIONSTRING="mongodb://localhost:27017"
export MONGODB__DATABASENAME="AutoFixer"

# New Relic
export NEWRELIC__ACCOUNTID="1234567890"
export NEWRELIC__APIKEY="NRIQ-xxxxxxxxxxxxxxxxxxxxx"
export NEWRELIC__APPLICATIONNAME="AutoFixer"

# Run application
dotnet run
```

## Docker Compose Setup

Create docker-compose.yml and run:
```bash
docker-compose up -d
```

This will start both MongoDB and AutoFixer together.