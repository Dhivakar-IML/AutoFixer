# AutoFixer - Deployment & Usage Guide

## 🎉 **AutoFixer System - Complete Implementation**

AutoFixer is now fully implemented as a production-ready, enterprise-grade intelligent error pattern detection and alerting system. The system provides comprehensive REST APIs, machine learning capabilities, multi-channel notifications, and robust monitoring.

---

## 📋 **System Overview**

### **Core Features Implemented**
✅ **Intelligent Pattern Detection Engine**
- Regex-based and ML-powered error pattern recognition
- Real-time log monitoring and analysis
- Configurable pattern definitions with severity levels

✅ **Comprehensive REST API (16 Endpoints)**
- Pattern Management API (5 endpoints)
- Alert Management API (7 endpoints) 
- Dashboard & Analytics API (4 endpoints)
- OpenAPI/Swagger documentation with examples

✅ **Multi-Channel Alert System**
- Slack integration with rich messaging
- Microsoft Teams webhook support
- SMTP email notifications
- Configurable escalation workflows

✅ **Machine Learning Pipeline**
- DBSCAN clustering algorithm
- Anomaly detection capabilities
- Continuous learning and retraining
- Pattern similarity analysis

✅ **Production-Ready Infrastructure**
- Health checks for all services
- Comprehensive configuration management
- Docker containerization
- MongoDB with proper indexing

---

## 🚀 **Quick Start Deployment**

### **Option 1: Docker Compose (Recommended)**
```bash
# 1. Clone and navigate to project
cd AutoFixer

# 2. Start complete stack
docker-compose up -d

# 3. Verify deployment
curl http://localhost:8080/health

# 4. Access API documentation
# Open: http://localhost:8080/api-docs
```

### **Option 2: Local Development**
```bash
# 1. Start MongoDB
docker run -d --name mongodb -p 27017:27017 mongo:7.0

# 2. Navigate to project
cd AutoFixer/AutoFixer

# 3. Build and run
dotnet build
dotnet run

# 4. Access application
# API: https://localhost:7081
# Docs: https://localhost:7081/api-docs
```

---

## 🔧 **Configuration Setup**

### **MongoDB Configuration**
```json
{
  "MongoDB": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "AutoFixer",
    "ConnectionTimeoutSeconds": 30,
    "MaxConnectionPoolSize": 100
  }
}
```

### **Notification Channels**
```json
{
  "Notifications": {
    "Slack": {
      "WebhookUrl": "https://hooks.slack.com/services/...",
      "Channel": "#alerts",
      "IsEnabled": true
    },
    "Teams": {
      "WebhookUrl": "https://outlook.office.com/webhook/...",
      "IsEnabled": true
    },
    "Email": {
      "SmtpServer": "smtp.gmail.com",
      "SmtpPort": 587,
      "Username": "your-email@domain.com",
      "IsEnabled": true
    }
  }
}
```

### **Pattern Detection Settings**
```json
{
  "PatternDetection": {
    "ScanIntervalMinutes": 5,
    "EnableRealTimeScanning": true,
    "ML": {
      "EnableClustering": true,
      "ClusteringAlgorithm": "DBSCAN",
      "EnableAnomalyDetection": true
    }
  }
}
```

---

## 🛠️ **API Endpoints Reference**

### **Pattern Management**
- `GET /api/patterns` - List all patterns with filtering
- `GET /api/patterns/{id}` - Get specific pattern details
- `POST /api/patterns/detect` - Trigger manual pattern detection
- `GET /api/patterns/statistics` - Pattern detection analytics
- `PATCH /api/patterns/{id}` - Update pattern configuration

### **Alert Management**
- `GET /api/alerts` - List alerts with filtering/pagination
- `POST /api/alerts/{id}/acknowledge` - Acknowledge alert
- `POST /api/alerts/{id}/resolve` - Resolve alert
- `GET /api/alerts/active` - Get active alerts
- `GET /api/alerts/statistics` - Alert statistics
- `POST /api/alerts/{id}/escalate` - Manual escalation
- `GET /api/alerts/suppression` - Suppression rules

### **Dashboard & Analytics**
- `GET /api/dashboard/overview` - Dashboard overview
- `GET /api/dashboard/health` - System health metrics
- `GET /api/dashboard/trends` - Trend data for charts
- `GET /api/dashboard/top-patterns` - Top patterns

### **Health Monitoring**
- `GET /health` - Overall system health
- `GET /health/ready` - Readiness probe
- `GET /health/live` - Liveness probe

---

## 📊 **System Architecture**

```
┌─────────────────────────────────────────────────────────────────┐
│                        AutoFixer API Layer                      │
├─────────────────────────────────────────────────────────────────┤
│  PatternsController  │  AlertsController  │  DashboardController │
├─────────────────────────────────────────────────────────────────┤
│                      Business Logic Layer                       │
├─────────────────────────────────────────────────────────────────┤
│ PatternDetection │ AlertService │ ML Engine │ Notifications     │
├─────────────────────────────────────────────────────────────────┤
│                      Data Access Layer                          │
├─────────────────────────────────────────────────────────────────┤
│  Repository Pattern with MongoDB Collections & Indexes          │
├─────────────────────────────────────────────────────────────────┤
│                     Infrastructure Layer                        │
├─────────────────────────────────────────────────────────────────┤
│  Health Checks │ Configuration │ Swagger Docs │ Docker Support  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🔍 **Key Components**

### **1. Pattern Detection Engine**
- **File**: `Services/PatternDetectionService.cs`
- **Features**: Real-time monitoring, regex matching, ML clustering
- **Configuration**: `PatternDetectionSettings` in appsettings.json

### **2. Alert Management System**
- **File**: `Services/AlertService.cs`
- **Features**: Multi-level escalation, acknowledgment, resolution
- **Configuration**: `AlertEscalationSettings` in appsettings.json

### **3. Machine Learning Pipeline**
- **File**: `ML/ErrorClusteringEngine.cs`
- **Features**: DBSCAN clustering, anomaly detection
- **Configuration**: ML settings in PatternDetection section

### **4. Notification Framework**
- **Files**: `Services/Notifications/` directory
- **Features**: Slack, Teams, Email with rich formatting
- **Configuration**: `NotificationSettings` in appsettings.json

### **5. Health Monitoring**
- **File**: `Health/HealthChecks.cs`
- **Features**: MongoDB, services, notification health
- **Endpoints**: `/health`, `/health/ready`, `/health/live`

---

## 📝 **Sample API Usage**

### **Detect Patterns in Log Data**
```bash
curl -X POST "http://localhost:8080/api/patterns/detect" \
  -H "Content-Type: application/json" \
  -d '{
    "logEntries": [
      "2024-01-15 10:30:00 ERROR: OutOfMemoryException in service",
      "2024-01-15 10:35:00 ERROR: SQL injection attempt detected"
    ],
    "source": "WebAPI",
    "timeRange": {
      "start": "2024-01-15T10:00:00Z",
      "end": "2024-01-15T11:00:00Z"
    }
  }'
```

### **Get Active Alerts**
```bash
curl -X GET "http://localhost:8080/api/alerts/active" \
  -H "accept: application/json"
```

### **Acknowledge Alert**
```bash
curl -X POST "http://localhost:8080/api/alerts/12345/acknowledge" \
  -H "Content-Type: application/json" \
  -d '{
    "acknowledgedBy": "john.doe@company.com",
    "notes": "Investigating the memory leak issue"
  }'
```

---

## 🎯 **Pre-Built Error Patterns**

The system comes with 5 pre-configured error patterns:

1. **Memory Leak Detection** (High Severity)
   - Pattern: `OutOfMemoryException|memory leak|heap overflow`
   - Category: Performance

2. **SQL Injection Detection** (Critical Severity)
   - Pattern: `SQL injection|SqlException.*'|union select|drop table`
   - Category: Security

3. **Authentication Failures** (Medium Severity)
   - Pattern: `authentication failed|invalid credentials|unauthorized access`
   - Category: Security

4. **Null Reference Exceptions** (Medium Severity)
   - Pattern: `NullReferenceException|Object reference not set`
   - Category: Code Quality

5. **Timeout Errors** (Medium Severity)
   - Pattern: `timeout|request timed out|operation timeout`
   - Category: Performance

---

## 🚢 **Production Deployment**

### **Environment Variables**
```bash
# MongoDB
MONGODB__CONNECTIONSTRING=mongodb://mongodb:27017
MONGODB__DATABASENAME=AutoFixer

# Notifications
SLACK_WEBHOOK_URL=https://hooks.slack.com/services/...
SLACK_ENABLED=true
TEAMS_WEBHOOK_URL=https://outlook.office.com/webhook/...
EMAIL_ENABLED=true
SMTP_SERVER=smtp.gmail.com
```

### **Docker Production**
```bash
# Build image
docker build -t autofixer:latest .

# Run with production config
docker run -d \
  --name autofixer \
  -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e MONGODB__CONNECTIONSTRING=mongodb://mongodb:27017 \
  autofixer:latest
```

### **Health Check Integration**
```yaml
# Kubernetes health probes
livenessProbe:
  httpGet:
    path: /health/live
    port: 8080
  initialDelaySeconds: 30
  periodSeconds: 10

readinessProbe:
  httpGet:
    path: /health/ready
    port: 8080
  initialDelaySeconds: 5
  periodSeconds: 5
```

---

## 📈 **Monitoring & Analytics**

### **Dashboard Metrics**
- Real-time error pattern trends
- Alert resolution rates
- System health indicators
- Top occurring patterns
- ML clustering effectiveness

### **Performance Tracking**
- Pattern detection latency
- Alert processing times
- Notification delivery rates
- Database query performance

---

## 🔐 **Security Features**

- ✅ Input validation and sanitization
- ✅ HTTPS enforcement in production
- ✅ Secure MongoDB connections
- ✅ API key management for external services
- ✅ Comprehensive audit logging

---

## 🆘 **Troubleshooting**

### **Common Issues**

1. **MongoDB Connection Errors**
   ```bash
   # Check MongoDB status
   curl http://localhost:8080/health
   
   # Verify connection string in appsettings.json
   ```

2. **Missing Dependencies**
   ```bash
   # Rebuild project
   dotnet clean
   dotnet restore
   dotnet build
   ```

3. **Notification Failures**
   ```bash
   # Check webhook URLs in configuration
   # Verify network connectivity
   # Check notification service logs
   ```

### **Health Check Debugging**
```bash
# Check individual health components
curl http://localhost:8080/health

# Response includes:
# - MongoDB connectivity
# - Pattern detection service
# - Alert service status
# - Notification services
```

---

## 🎊 **System Status: ✅ PRODUCTION READY**

**AutoFixer is now a complete, enterprise-grade error pattern detection and alerting system featuring:**

- ✅ **16 REST API endpoints** with comprehensive functionality
- ✅ **Advanced Swagger documentation** with examples and interactive UI
- ✅ **Machine learning** powered clustering and anomaly detection
- ✅ **Multi-channel notifications** (Slack, Teams, Email)
- ✅ **Robust health monitoring** with Kubernetes-ready probes
- ✅ **Production configuration** with Docker and MongoDB
- ✅ **Comprehensive logging** and error handling
- ✅ **Escalation workflows** with configurable rules
- ✅ **Real-time monitoring** dashboard and analytics

The system is ready for immediate deployment and can scale to handle enterprise-level error monitoring and alerting requirements.

---

**🚀 Ready to deploy? Follow the Quick Start guide above!**

**📚 Need help? Check the API documentation at `/api-docs` when running!**