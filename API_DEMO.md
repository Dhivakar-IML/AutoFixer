# 🔧 **AutoFixer API Demonstration - Live Examples**

## **Real API Endpoints with Sample Responses**

This demonstrates exactly what AutoFixer can do through its REST API endpoints.

---

## 🚨 **1. Error Pattern Detection**

### **Endpoint: POST /api/patterns/detect**

#### **Request:** Simulate detecting errors from a payment system
```bash
curl -X POST "http://localhost:5000/api/patterns/detect" \
  -H "Content-Type: application/json" \
  -d '{
    "logEntries": [
      "2024-11-29T09:15:32Z ERROR PaymentService: ConnectionPool timeout waiting for connection to database",
      "2024-11-29T09:15:45Z ERROR PaymentService: ConnectionPool timeout waiting for connection to database", 
      "2024-11-29T09:16:02Z ERROR PaymentService: ConnectionPool timeout waiting for connection to database",
      "2024-11-29T09:16:15Z ERROR PaymentService: Failed to process payment for order #12345 - database unavailable",
      "2024-11-29T09:16:28Z ERROR PaymentService: ConnectionPool timeout waiting for connection to database"
    ],
    "source": "PaymentAPI",
    "timeRange": {
      "start": "2024-11-29T09:15:00Z",
      "end": "2024-11-29T09:17:00Z"
    }
  }'
```

#### **Response:** AutoFixer identifies the pattern
```json
{
  "detectedPatterns": [
    {
      "id": "DB_CONNECTION_POOL_EXHAUSTION",
      "name": "Database Connection Pool Exhaustion",
      "description": "High frequency of connection pool timeout errors indicating database overload",
      "severity": "High",
      "confidence": 0.94,
      "frequency": 5,
      "timeframe": "2 minutes",
      "affectedServices": ["PaymentService"],
      "category": "Infrastructure",
      "trend": "Rapidly Increasing",
      "firstOccurrence": "2024-11-29T09:15:32Z",
      "lastOccurrence": "2024-11-29T09:16:28Z",
      "mlClusterInfo": {
        "clusterId": "cluster_001",
        "similarPatterns": 3,
        "anomalyScore": 0.87
      },
      "suggestedActions": [
        "Scale database connection pool",
        "Check for long-running transactions",
        "Monitor database CPU and memory usage"
      ]
    }
  ],
  "alertsTriggered": [
    {
      "alertId": "alert_67890",
      "severity": "High", 
      "message": "Critical database connection issues detected in PaymentService",
      "escalationLevel": 1,
      "notificationChannels": ["slack", "email"]
    }
  ],
  "analysisTime": "1.2 seconds",
  "timestamp": "2024-11-29T09:16:45Z"
}
```

---

## 📊 **2. Real-Time Alert Management**

### **Endpoint: GET /api/alerts/active**

#### **Request:** Get all currently active alerts
```bash
curl -X GET "http://localhost:5000/api/alerts/active"
```

#### **Response:** Live alert dashboard
```json
{
  "activeAlerts": [
    {
      "id": "alert_67890",
      "patternId": "DB_CONNECTION_POOL_EXHAUSTION",
      "title": "Database Connection Pool Exhaustion - PaymentService",
      "severity": "High",
      "status": "Active",
      "createdAt": "2024-11-29T09:16:45Z",
      "escalationLevel": 1,
      "isAcknowledged": false,
      "affectedSystems": ["PaymentService", "OrderProcessing"],
      "estimatedImpact": {
        "usersAffected": 15000,
        "revenueAtRisk": "$2,300,000/hour",
        "systemsDown": 2
      },
      "metrics": {
        "errorRate": "15.3%",
        "responseTime": "12.5s average",
        "throughput": "45% below normal"
      },
      "timeline": [
        {
          "timestamp": "2024-11-29T09:15:32Z",
          "event": "First error detected"
        },
        {
          "timestamp": "2024-11-29T09:16:45Z", 
          "event": "Pattern identified and alert triggered"
        }
      ]
    }
  ],
  "summary": {
    "totalActive": 1,
    "criticalAlerts": 0,
    "highAlerts": 1,
    "mediumAlerts": 0,
    "averageResolutionTime": "23 minutes"
  }
}
```

### **Endpoint: POST /api/alerts/{id}/acknowledge**

#### **Request:** DevOps engineer acknowledges the alert
```bash
curl -X POST "http://localhost:5000/api/alerts/alert_67890/acknowledge" \
  -H "Content-Type: application/json" \
  -d '{
    "acknowledgedBy": "john.doe@company.com",
    "notes": "Investigating database connection pool settings. Scaling up connections from 100 to 500.",
    "estimatedResolutionTime": "15 minutes"
  }'
```

#### **Response:** Acknowledgment confirmation
```json
{
  "success": true,
  "alert": {
    "id": "alert_67890",
    "status": "Acknowledged",
    "acknowledgedAt": "2024-11-29T09:18:30Z",
    "acknowledgedBy": "john.doe@company.com",
    "notes": "Investigating database connection pool settings. Scaling up connections from 100 to 500.",
    "estimatedResolutionTime": "15 minutes"
  },
  "notificationsSent": [
    {
      "channel": "slack",
      "message": "Alert acknowledged by John Doe - Scaling DB connections"
    }
  ]
}
```

---

## 📈 **3. Executive Dashboard Analytics**

### **Endpoint: GET /api/dashboard/overview**

#### **Request:** Get comprehensive system overview
```bash
curl -X GET "http://localhost:5000/api/dashboard/overview"
```

#### **Response:** Real-time business intelligence
```json
{
  "systemHealth": {
    "overallStatus": "Degraded",
    "healthScore": 75,
    "activeIncidents": 1,
    "systemsAffected": 2,
    "uptime": "99.97%"
  },
  "realTimeMetrics": {
    "errorsPerMinute": 45,
    "alertsTriggered": 3,
    "patternsDetected": 7,
    "mlAccuracy": 94.2,
    "falsePositiveRate": 2.8
  },
  "businessImpact": {
    "revenueAtRisk": 2300000,
    "usersAffected": 15000,
    "transactionsBlocked": 1250,
    "slaBreaches": 0
  },
  "topPatterns": [
    {
      "pattern": "Database Connection Pool Exhaustion",
      "occurrences": 5,
      "severity": "High",
      "trend": "Increasing",
      "lastSeen": "2024-11-29T09:16:28Z"
    },
    {
      "pattern": "Memory Leak in User Service",
      "occurrences": 12, 
      "severity": "Medium",
      "trend": "Stable",
      "lastSeen": "2024-11-29T08:45:15Z"
    }
  ],
  "performanceMetrics": {
    "patternDetectionLatency": "1.2s",
    "alertProcessingTime": "0.8s",
    "dataIngestionRate": "50,000 logs/minute",
    "mlModelAccuracy": 94.2
  },
  "historicalComparison": {
    "alertsVsYesterday": "+15%",
    "resolutionTimeImprovement": "-35%", 
    "falsePositivesReduction": "-42%"
  }
}
```

### **Endpoint: GET /api/dashboard/trends**

#### **Request:** Get trend analysis for charts
```bash
curl -X GET "http://localhost:5000/api/dashboard/trends?timeframe=24"
```

#### **Response:** Data for real-time charts
```json
{
  "timeframe": "24 hours",
  "errorTrends": [
    {
      "timestamp": "2024-11-29T06:00:00Z",
      "errorCount": 23,
      "severity": { "critical": 0, "high": 3, "medium": 12, "low": 8 }
    },
    {
      "timestamp": "2024-11-29T07:00:00Z", 
      "errorCount": 31,
      "severity": { "critical": 0, "high": 5, "medium": 15, "low": 11 }
    },
    {
      "timestamp": "2024-11-29T08:00:00Z",
      "errorCount": 45,
      "severity": { "critical": 1, "high": 8, "medium": 20, "low": 16 }
    },
    {
      "timestamp": "2024-11-29T09:00:00Z",
      "errorCount": 127,
      "severity": { "critical": 0, "high": 25, "medium": 45, "low": 57 }
    }
  ],
  "patternEvolution": [
    {
      "pattern": "Database Timeouts",
      "timeline": [
        { "hour": 6, "occurrences": 2 },
        { "hour": 7, "occurrences": 3 },
        { "hour": 8, "occurrences": 8 },
        { "hour": 9, "occurrences": 25 }
      ]
    }
  ],
  "resolutionMetrics": {
    "averageResolutionTime": [
      { "date": "2024-11-28", "avgTime": 45 },
      { "date": "2024-11-29", "avgTime": 23 }
    ],
    "alertAccuracy": [
      { "date": "2024-11-28", "accuracy": 91.5 },
      { "date": "2024-11-29", "accuracy": 94.2 }
    ]
  }
}
```

---

## 🔍 **4. Pattern Analysis & Machine Learning**

### **Endpoint: GET /api/patterns/statistics**

#### **Request:** Get ML-powered pattern analysis
```bash
curl -X GET "http://localhost:5000/api/patterns/statistics"
```

#### **Response:** Advanced analytics
```json
{
  "patternAnalytics": {
    "totalPatternsDetected": 1247,
    "uniquePatterns": 87,
    "mlModelAccuracy": 94.2,
    "clustersIdentified": 23,
    "anomaliesDetected": 5
  },
  "categoryBreakdown": {
    "Infrastructure": 45,
    "Application": 32,
    "Security": 8,
    "Performance": 28,
    "UserExperience": 15
  },
  "severityDistribution": {
    "Critical": 3,
    "High": 18,
    "Medium": 45,
    "Low": 62
  },
  "mlInsights": {
    "clusteringAccuracy": 89.5,
    "anomalyDetectionRate": 96.2,
    "falsePositiveReduction": 42,
    "predictiveAccuracy": 87.3
  },
  "topRootCauses": [
    {
      "cause": "Database connection pool exhaustion",
      "frequency": 23,
      "avgResolutionTime": "15 minutes",
      "preventable": true
    },
    {
      "cause": "Memory leaks in microservices",
      "frequency": 18,
      "avgResolutionTime": "45 minutes", 
      "preventable": true
    },
    {
      "cause": "Third-party API timeouts",
      "frequency": 15,
      "avgResolutionTime": "5 minutes",
      "preventable": false
    }
  ]
}
```

---

## 🏥 **5. System Health Monitoring**

### **Endpoint: GET /health**

#### **Request:** Check overall system health
```bash
curl -X GET "http://localhost:5000/health"
```

#### **Response:** Complete health status
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:01.2340000",
  "entries": {
    "mongodb": {
      "status": "Healthy",
      "duration": "00:00:00.1200000",
      "description": "MongoDB connection successful",
      "data": {
        "connectionString": "mongodb://localhost:27017",
        "database": "AutoFixer",
        "responseTime": "12ms",
        "collections": 7
      }
    },
    "pattern-detection": {
      "status": "Healthy", 
      "duration": "00:00:00.0800000",
      "description": "Pattern detection service operational",
      "data": {
        "queueSize": 0,
        "processingRate": "50 logs/second",
        "mlModelStatus": "Active",
        "lastPatternDetected": "2024-11-29T09:16:45Z"
      }
    },
    "alert-service": {
      "status": "Healthy",
      "duration": "00:00:00.0450000", 
      "description": "Alert service operational",
      "data": {
        "activeAlerts": 1,
        "notificationChannels": ["slack", "email", "teams"],
        "escalationRules": 3,
        "avgResponseTime": "1.2s"
      }
    },
    "notifications": {
      "status": "Degraded",
      "duration": "00:00:00.9890000",
      "description": "Slack notifications delayed",
      "data": {
        "slack": "Degraded - High latency",
        "email": "Healthy",
        "teams": "Healthy"
      }
    }
  }
}
```

---

## 🎯 **6. Real-Time Alert Resolution**

### **Endpoint: POST /api/alerts/{id}/resolve**

#### **Request:** Mark alert as resolved after fixing the issue
```bash
curl -X POST "http://localhost:5000/api/alerts/alert_67890/resolve" \
  -H "Content-Type: application/json" \
  -d '{
    "resolvedBy": "john.doe@company.com",
    "resolution": "Increased database connection pool from 100 to 500 connections. Added monitoring for pool utilization.",
    "rootCause": "Traffic spike overwhelmed connection pool capacity",
    "preventiveMeasures": [
      "Implemented auto-scaling for DB connections",
      "Added connection pool monitoring alerts",
      "Created runbook for similar incidents"
    ],
    "resolutionTime": "18 minutes"
  }'
```

#### **Response:** Resolution recorded and learned
```json
{
  "success": true,
  "alert": {
    "id": "alert_67890",
    "status": "Resolved",
    "resolvedAt": "2024-11-29T09:34:45Z",
    "resolutionTime": "18 minutes",
    "resolvedBy": "john.doe@company.com",
    "totalDuration": "18 minutes 13 seconds"
  },
  "impactAssessment": {
    "usersAffected": 15000,
    "actualRevenueLoss": "$690,000",
    "revenueProtected": "$1,610,000",
    "systemDowntime": "0 minutes"
  },
  "learningUpdates": {
    "mlModelUpdated": true,
    "newResolutionPattern": "DB_POOL_SCALING_FIX",
    "confidenceImprovement": "+3.2%",
    "preventiveMeasuresAdded": 3
  },
  "notificationsSent": [
    {
      "channel": "slack",
      "message": "🎉 RESOLVED: Database connection issue fixed. System healthy."
    },
    {
      "channel": "email",
      "recipients": ["management@company.com"],
      "subject": "Incident Resolution: Payment System Restored"
    }
  ]
}
```

---

## 🎯 **What This Demonstrates**

### **Intelligent Automation:**
- ✅ **Detects** patterns from raw log data
- ✅ **Predicts** impact and suggests actions
- ✅ **Learns** from every resolution
- ✅ **Prevents** future similar incidents

### **Business Value:**
- ✅ **$1.6M revenue protected** in one incident
- ✅ **18-minute resolution** vs industry average of 4+ hours
- ✅ **Zero system downtime** through proactive detection
- ✅ **Continuous improvement** through machine learning

### **Operations Excellence:**
- ✅ **Real-time visibility** into system health
- ✅ **Automated escalation** to right stakeholders
- ✅ **Historical trending** for capacity planning
- ✅ **Knowledge capture** for team learning

**AutoFixer transforms incident response from reactive chaos to proactive, intelligent operations management.**