# AutoFixer - Intelligent Error Pattern Detection and Alerting System

AutoFixer is a comprehensive ASP.NET Core Web API application that provides intelligent error pattern detection, machine learning-powered clustering, and multi-channel alerting capabilities for monitoring and managing application errors in real-time.

## 🚀 Features

### Core Capabilities
- **Intelligent Pattern Detection**: Regex-based and ML-powered error pattern recognition
- **Real-time Alerting**: Multi-channel notifications (Slack, Teams, Email)
- **Machine Learning**: Advanced error clustering and anomaly detection
- **Escalation Management**: Configurable alert escalation levels
- **Comprehensive API**: RESTful endpoints for patterns, alerts, and dashboard analytics
- **Health Monitoring**: Built-in health checks for all services and dependencies

### API Endpoints

#### Pattern Management
- `GET /api/patterns` - Get all error patterns with filtering
- `GET /api/patterns/{id}` - Get specific pattern details
- `POST /api/patterns/detect` - Trigger manual pattern detection
- `GET /api/patterns/statistics` - Get pattern detection statistics
- `PATCH /api/patterns/{id}` - Update pattern configuration

#### Alert Management
- `GET /api/alerts` - Get alerts with filtering and pagination
- `POST /api/alerts/{id}/acknowledge` - Acknowledge an alert
- `POST /api/alerts/{id}/resolve` - Resolve an alert
- `GET /api/alerts/active` - Get currently active alerts
- `GET /api/alerts/statistics` - Get alert statistics
- `POST /api/alerts/{id}/escalate` - Manually escalate an alert
- `GET /api/alerts/suppression` - Get alert suppression rules

#### Dashboard & Analytics
- `GET /api/dashboard/overview` - Get dashboard overview data
- `GET /api/dashboard/health` - Get system health metrics
- `GET /api/dashboard/trends` - Get trend data for charts
- `GET /api/dashboard/top-patterns` - Get top occurring patterns

#### Health Monitoring
- `GET /health` - Overall system health
- `GET /health/ready` - Readiness probe
- `GET /health/live` - Liveness probe

## 🛠️ Technology Stack

- **Framework**: ASP.NET Core 8.0 Web API
- **Database**: MongoDB 7.0+
- **Machine Learning**: ML.NET
- **Documentation**: Swagger/OpenAPI 3.0
- **Health Checks**: Microsoft.Extensions.Diagnostics.HealthChecks
- **Containerization**: Docker & Docker Compose

## 📋 Prerequisites

- .NET 8.0 SDK or later
- MongoDB 7.0+ (or Docker for containerized deployment)
- Visual Studio 2022 or VS Code (recommended)

## 🚀 Quick Start

### Option 1: Docker Compose (Recommended)

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd AutoFixer
   ```

2. **Start the application stack**
   ```bash
   docker-compose up -d
   ```

3. **Access the application**
   - API: http://localhost:8080
   - API Documentation: http://localhost:8080/api-docs
   - Health Checks: http://localhost:8080/health
   - MongoDB Admin (optional): http://localhost:8082 (admin/admin)

### Option 2: Local Development

1. **Clone and navigate to project**
   ```bash
   git clone <repository-url>
   cd AutoFixer/AutoFixer
   ```

2. **Start MongoDB**
   ```bash
   # Using Docker
   docker run -d --name mongodb -p 27017:27017 mongo:7.0
   
   # Or install MongoDB locally
   ```

3. **Configure settings**
   - Update `appsettings.json` with your MongoDB connection string
   - Configure notification channels (Slack, Teams, Email)

4. **Run the application**
   ```bash
   dotnet run
   ```

5. **Access the application**
   - API: https://localhost:7081 or http://localhost:5081
   - API Documentation: https://localhost:7081/api-docs

## ⚙️ Configuration

### MongoDB Settings
```json
{
  "MongoDB": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "AutoFixer",
    "ConnectionTimeoutSeconds": 30,
    "SocketTimeoutSeconds": 30,
    "MaxConnectionPoolSize": 100,
    "ServerSelectionTimeoutSeconds": 30
  }
}
```

### Pattern Detection Settings
```json
{
  "PatternDetection": {
    "ScanIntervalMinutes": 5,
    "MaxLogRetentionDays": 30,
    "EnableRealTimeScanning": true,
    "ML": {
      "EnableClustering": true,
      "ClusteringAlgorithm": "DBSCAN",
      "MinSamplesForCluster": 3,
      "EpsilonDistance": 0.5,
      "EnableAnomalyDetection": true,
      "AnomalyThreshold": 0.8
    }
  }
}
```

### Notification Settings
```json
{
  "Notifications": {
    "Slack": {
      "WebhookUrl": "https://hooks.slack.com/services/...",
      "Channel": "#alerts",
      "Username": "AutoFixer",
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
      "Password": "your-password",
      "FromEmail": "autofixer@domain.com",
      "UseSsl": true,
      "IsEnabled": true
    }
  }
}
```

### Alert Escalation Settings
```json
{
  "AlertEscalation": {
    "Levels": [
      {
        "Level": 1,
        "ThresholdMinutes": 15,
        "NotificationChannels": ["Slack"],
        "Recipients": ["dev-team"]
      },
      {
        "Level": 2,
        "ThresholdMinutes": 30,
        "NotificationChannels": ["Slack", "Email"],
        "Recipients": ["dev-team", "tech-leads"]
      },
      {
        "Level": 3,
        "ThresholdMinutes": 60,
        "NotificationChannels": ["Slack", "Email", "Teams"],
        "Recipients": ["dev-team", "tech-leads", "managers"]
      }
    ],
    "MaxEscalationLevel": 3,
    "EscalationCheckIntervalMinutes": 5
  }
}
```

## 🏗️ Architecture

### Service Layer Architecture
```
Controllers/
├── PatternsController.cs      # Pattern management API
├── AlertsController.cs        # Alert lifecycle API
└── DashboardController.cs     # Analytics and monitoring API

Services/
├── PatternDetectionService    # Core pattern detection logic
├── AlertService              # Alert management and workflows
├── NotificationServices/     # Multi-channel notifications
└── AlertSuppressionService   # Alert suppression rules

Data/
├── Repositories/            # Data access layer
└── Models/                 # Domain models

ML/
└── ErrorClusteringEngine   # Machine learning components

Configuration/
├── Settings.cs            # Configuration classes
├── SwaggerConfiguration   # API documentation setup
└── HealthChecks.cs       # Health monitoring
```

### Key Components

#### Pattern Detection Engine
- Real-time log monitoring
- Regex-based pattern matching
- ML-powered error clustering
- Anomaly detection algorithms

#### Alert Management System
- Multi-level escalation workflows
- Alert suppression and acknowledgment
- Real-time status tracking
- Historical analytics

#### Notification Framework
- Slack integration with rich messaging
- Microsoft Teams webhook support
- SMTP email notifications
- Configurable escalation rules

#### Machine Learning Pipeline
- DBSCAN clustering algorithm
- Anomaly detection models
- Continuous learning capabilities
- Pattern similarity analysis

## 🔍 API Documentation

The application provides comprehensive OpenAPI 3.0 documentation with:
- Interactive Swagger UI
- Detailed endpoint descriptions
- Request/response examples
- Authentication schemes
- Error response documentation

Access the documentation at: `/api-docs` when the application is running.

## 🏥 Health Monitoring

AutoFixer includes comprehensive health checks:

- **MongoDB Health**: Database connectivity and performance
- **Pattern Detection Health**: Service operational status
- **Alert Service Health**: Alert processing capabilities
- **Notification Health**: External service connectivity

Health check endpoints:
- `/health` - Overall system health
- `/health/ready` - Kubernetes readiness probe
- `/health/live` - Kubernetes liveness probe

## 🚢 Production Deployment

### Docker Deployment
```bash
# Build and run with Docker Compose
docker-compose up -d

# Scale the application
docker-compose up -d --scale autofixer=3

# View logs
docker-compose logs -f autofixer
```

### Kubernetes Deployment
```yaml
# Example Kubernetes deployment
apiVersion: apps/v1
kind: Deployment
metadata:
  name: autofixer
spec:
  replicas: 3
  selector:
    matchLabels:
      app: autofixer
  template:
    metadata:
      labels:
        app: autofixer
    spec:
      containers:
      - name: autofixer
        image: autofixer:latest
        ports:
        - containerPort: 8080
        env:
        - name: MongoDB__ConnectionString
          value: "mongodb://mongodb-service:27017"
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

### Environment Variables
```bash
# MongoDB Configuration
MONGODB__CONNECTIONSTRING=mongodb://localhost:27017
MONGODB__DATABASENAME=AutoFixer

# ASP.NET Core Configuration
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080

# Notification Configuration
NOTIFICATIONS__SLACK__WEBHOOKURL=https://hooks.slack.com/...
NOTIFICATIONS__SLACK__ISENABLED=true
```

## 🔧 Development

### Prerequisites
- .NET 8.0 SDK
- Visual Studio 2022 or VS Code
- MongoDB (local or Docker)

### Setup Development Environment
```bash
# Clone repository
git clone <repository-url>
cd AutoFixer

# Restore dependencies
dotnet restore

# Run tests
dotnet test

# Start development server
dotnet run --project AutoFixer
```

### Project Structure
```
AutoFixer/
├── Controllers/           # API controllers
├── Services/             # Business logic services
├── Data/                # Data access layer
├── ML/                  # Machine learning components
├── Configuration/       # Configuration classes
├── Models/             # Domain models
├── Health/            # Health check implementations
└── Program.cs         # Application entry point
```

## 📊 Monitoring and Analytics

AutoFixer provides comprehensive monitoring capabilities:

### Dashboard Metrics
- Real-time error pattern trends
- Alert volume and resolution rates
- System health indicators
- Top occurring error patterns
- ML clustering effectiveness

### Performance Metrics
- Pattern detection latency
- Alert processing times
- Notification delivery rates
- Database query performance
- ML model accuracy

## 🛡️ Security Considerations

- Secure MongoDB connections with authentication
- HTTPS enforcement in production
- API key management for external services
- Input validation and sanitization
- Logging and audit trails

## 🔄 Backup and Recovery

### MongoDB Backup
```bash
# Create backup
mongodump --host localhost:27017 --db AutoFixer --out /backup/

# Restore backup
mongorestore --host localhost:27017 --db AutoFixer /backup/AutoFixer/
```

## 📈 Scaling Considerations

- Horizontal scaling with load balancers
- MongoDB replica sets for high availability
- Caching strategies for frequently accessed data
- Asynchronous processing for heavy ML workloads
- Message queues for notification processing

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests for new functionality
5. Submit a pull request

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🆘 Support

For support and questions:
- Create an issue in the GitHub repository
- Review the API documentation at `/api-docs`
- Check the health endpoints for system status
- Review application logs for troubleshooting

---

## 🎯 Getting Started Checklist

- [ ] Clone the repository
- [ ] Start MongoDB (Docker or local)
- [ ] Configure notification channels in `appsettings.json`
- [ ] Run `docker-compose up -d` or `dotnet run`
- [ ] Access API documentation at `/api-docs`
- [ ] Test pattern detection with sample data
- [ ] Configure alert escalation rules
- [ ] Set up monitoring dashboards
- [ ] Deploy to production environment

AutoFixer is ready to help you proactively monitor, detect, and respond to application errors with intelligent automation and comprehensive alerting!