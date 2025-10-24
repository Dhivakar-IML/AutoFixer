# AutoFixer - MongoDB & New Relic Configuration Guide

## 🗄️ **MongoDB Configuration**

### **Step 1: Install MongoDB**

#### **Option A: Docker (Recommended for Development)**
```bash
# Start MongoDB with proper configuration for AutoFixer
docker run -d --name autofixer-mongodb \
  -p 27017:27017 \
  -e MONGO_INITDB_ROOT_USERNAME=admin \
  -e MONGO_INITDB_ROOT_PASSWORD=password \
  -e MONGO_INITDB_DATABASE=AutoFixer \
  -v mongodb_data:/data/db \
  mongo:7.0

# Initialize with sample data
docker exec -i autofixer-mongodb mongosh AutoFixer < mongo-init.js
```

#### **Option B: MongoDB Atlas (Cloud)**
1. Sign up at [MongoDB Atlas](https://www.mongodb.com/cloud/atlas)
2. Create a cluster
3. Get connection string: `mongodb+srv://username:password@cluster.mongodb.net/AutoFixer`
4. Whitelist your IP address

#### **Option C: Local Installation**
- **Windows**: Download from [mongodb.com](https://www.mongodb.com/try/download/community)
- **macOS**: `brew install mongodb-community@7.0`
- **Linux**: Follow [official guide](https://docs.mongodb.com/manual/installation/)

### **Step 2: Configure Connection String**

Update `appsettings.json`:

```json
{
  "MongoDB": {
    "ConnectionString": "mongodb://localhost:27017",  // Local
    // OR for Atlas: "mongodb+srv://username:password@cluster.mongodb.net/AutoFixer"
    // OR for authenticated: "mongodb://admin:password@localhost:27017"
    "DatabaseName": "AutoFixer",
    "ConnectionTimeoutSeconds": 30,
    "SocketTimeoutSeconds": 30,
    "MaxConnectionPoolSize": 100,
    "ServerSelectionTimeoutSeconds": 30,
    "UseSsl": false  // Set to true for Atlas
  }
}
```

### **Step 3: Production Configuration**

For production with authentication:
```json
{
  "MongoDB": {
    "ConnectionString": "mongodb://admin:password@mongodb:27017/AutoFixer?authSource=admin",
    "DatabaseName": "AutoFixer",
    "UseSsl": true,
    "ConnectionTimeoutSeconds": 30,
    "MaxConnectionPoolSize": 100
  }
}
```

### **Step 4: Environment Variables (Docker/K8s)**
```bash
# Set these environment variables
MONGODB__CONNECTIONSTRING=mongodb://admin:password@mongodb:27017
MONGODB__DATABASENAME=AutoFixer
MONGODB__USESSL=false
```

---

## 📊 **New Relic Configuration**

### **Step 1: Get New Relic Account**
1. Sign up at [New Relic](https://newrelic.com/)
2. Create an application
3. Get your **Account ID** and **API Key**

### **Step 2: Configure New Relic Settings**

Update `appsettings.json`:
```json
{
  "NewRelic": {
    "AccountId": "YOUR_ACCOUNT_ID",           // From New Relic dashboard
    "ApiKey": "YOUR_INSIGHTS_QUERY_KEY",     // Insights Query Key
    "BaseUrl": "https://insights-api.newrelic.com",
    "ApplicationName": "AutoFixer",
    "TimeoutSeconds": 30,
    "MaxEventsPerRequest": 10000
  }
}
```

### **Step 3: Find Your New Relic Credentials**

#### **Account ID:**
- Go to New Relic dashboard
- Click on your account dropdown (top right)
- Account ID is shown in the account info

#### **API Key:**
- Go to [API Keys page](https://one.newrelic.com/launcher/api-keys-ui.api-keys-launcher)
- Create or copy an **Insights Query Key**
- Use this as your `ApiKey` value

### **Step 4: Production Environment Variables**
```bash
# Set these environment variables
NEWRELIC__ACCOUNTID=1234567890
NEWRELIC__APIKEY=NRIQ-xxxxxxxxxxxxxxxxxxxxx
NEWRELIC__APPLICATIONNAME=AutoFixer-Production
```

---

## 🧪 **Testing Configurations**

### **Test MongoDB Connection**
```bash
# Start the application
dotnet run

# Test health endpoint
curl http://localhost:5000/health

# Should return:
{
  "status": "Healthy",
  "checks": {
    "mongodb": "Healthy",
    "pattern-detection": "Healthy",
    "alert-service": "Healthy",
    "notifications": "Healthy"
  }
}
```

### **Test New Relic Integration**
```bash
# Call the pattern detection endpoint to trigger New Relic ingestion
curl -X POST "http://localhost:5000/api/patterns/detect" \
  -H "Content-Type: application/json" \
  -d '{
    "logEntries": ["Sample error message"],
    "source": "TestAPI"
  }'
```

---

## 🚀 **Quick Start Commands**

### **Start MongoDB with Docker:**
```bash
docker run -d --name autofixer-mongodb -p 27017:27017 mongo:7.0
```

### **Start AutoFixer with MongoDB:**
```bash
cd AutoFixer/AutoFixer
dotnet run
```

### **Initialize MongoDB with Sample Data:**
```bash
# Copy the mongo-init.js file and run:
mongosh AutoFixer < mongo-init.js
```

---

## 🔧 **Advanced Configuration**

### **MongoDB with Authentication:**
```json
{
  "MongoDB": {
    "ConnectionString": "mongodb://admin:password@localhost:27017/AutoFixer?authSource=admin&ssl=true",
    "DatabaseName": "AutoFixer",
    "UseSsl": true
  }
}
```

### **New Relic with Custom NRQL Queries:**
The application automatically generates NRQL queries like:
```sql
SELECT timestamp, message, `error.class`, `error.message`, appName, host 
FROM Log 
WHERE appName = 'AutoFixer' 
AND timestamp >= 1640995200000 
AND level = 'ERROR' 
LIMIT 10000
```

### **Multiple Environment Support:**

#### **appsettings.Development.json:**
```json
{
  "MongoDB": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "AutoFixer_Dev"
  },
  "NewRelic": {
    "AccountId": "DEV_ACCOUNT_ID",
    "ApplicationName": "AutoFixer-Dev"
  }
}
```

#### **appsettings.Production.json:**
```json
{
  "MongoDB": {
    "ConnectionString": "mongodb://prod-mongodb:27017",
    "DatabaseName": "AutoFixer_Prod",
    "UseSsl": true
  },
  "NewRelic": {
    "AccountId": "PROD_ACCOUNT_ID",
    "ApplicationName": "AutoFixer-Production"
  }
}
```

---

## 🏥 **Health Checks**

The application includes health checks for both services:

### **MongoDB Health Check:**
- Tests database connectivity
- Validates collection access
- Checks query performance

### **New Relic Health Check:**
- Tests API connectivity
- Validates authentication
- Checks query permissions

Access health checks at: `http://localhost:5000/health`

---

## 🐛 **Troubleshooting**

### **MongoDB Issues:**
```bash
# Check MongoDB is running
docker ps | grep mongo

# Check MongoDB logs
docker logs autofixer-mongodb

# Test connection manually
mongosh mongodb://localhost:27017/AutoFixer
```

### **New Relic Issues:**
- Verify Account ID and API Key are correct
- Check application name matches in New Relic dashboard
- Ensure Insights Query Key has proper permissions
- Verify network connectivity to New Relic API

### **Common Errors:**
- `MongoDB connection failed`: Check connection string and MongoDB service
- `New Relic authentication failed`: Verify API key and account ID
- `Health check unhealthy`: Check both MongoDB and New Relic configurations

---

## 📝 **Configuration Checklist**

- [ ] MongoDB installed and running
- [ ] Connection string updated in appsettings.json
- [ ] Database name configured
- [ ] New Relic account created
- [ ] Account ID obtained
- [ ] Insights Query API key generated
- [ ] Application name set
- [ ] Health checks passing
- [ ] Sample data ingested successfully

Your AutoFixer application is now ready with MongoDB and New Relic integration!