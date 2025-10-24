# AutoFixer Demo Guide

This demo showcases the complete AutoFixer intelligent error pattern detection and alerting system.

## Prerequisites

1. **AutoFixer Application**: Ensure the AutoFixer application is running
   ```bash
   cd AutoFixer
   dotnet run --urls "http://localhost:5000"
   ```

2. **MongoDB**: Ensure MongoDB is running (Docker container recommended)
   ```bash
   docker ps | grep mongo
   ```

3. **PowerShell**: Windows PowerShell 5.1+ or PowerShell Core 7+

## Quick Start

### Option 1: Run with Batch File (Easiest)
```batch
run-demo.bat
```

### Option 2: Run PowerShell Script Directly
```powershell
.\demo-script.ps1
```

### Option 3: Run with Custom Parameters
```powershell
# Detailed output with existing data
.\demo-script.ps1 -Detailed -SkipDataGeneration

# Custom base URL
.\demo-script.ps1 -BaseUrl "http://localhost:8080"

# Full demo with detailed output
.\demo-script.ps1 -Detailed
```

## Demo Features

The demo demonstrates the following AutoFixer capabilities:

### 🏥 **System Health**
- Application health checks
- MongoDB connectivity verification
- Service availability validation

### 📊 **Data Management**
- Sample data generation with realistic error patterns
- Multiple error types (Critical, High, Medium, Low priority)
- Various error categories and affected services

### 🔍 **Pattern Detection**
- Intelligent error pattern clustering using ML.NET
- TF-IDF text featurization for error message analysis
- DBSCAN clustering for grouping similar errors
- Confidence scoring for pattern reliability

### 🎛️ **Advanced Filtering**
- Filter by priority level (Low, Medium, High, Critical)
- Filter by pattern type (Recurring, Persistent, Spike, etc.)
- Filter by confidence threshold (0-100%)
- Filter by time range (last N hours)

### 📋 **Detailed Analysis**
- Individual pattern deep-dive
- Affected services mapping
- Occurrence rate tracking
- User impact analysis

### 🔔 **Alert Management**
- Real-time alert generation
- Multi-severity alert classification
- Alert status tracking
- Notification channel integration

### 📊 **Dashboard Analytics**
- System overview metrics
- Pattern trend analysis
- Critical issue identification
- Health status monitoring

### 🧠 **Machine Learning Insights**
- ML-powered error clustering
- Anomaly detection capabilities
- Pattern evolution tracking
- Similarity scoring algorithms

## Demo Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `-BaseUrl` | AutoFixer API base URL | `http://localhost:5000` |
| `-SkipDataGeneration` | Skip sample data creation | `false` |
| `-Detailed` | Show detailed output and analysis | `false` |

## Sample Commands

```powershell
# Basic demo
.\demo-script.ps1

# Detailed demo with full output
.\demo-script.ps1 -Detailed

# Use existing data without regeneration
.\demo-script.ps1 -SkipDataGeneration -Detailed

# Demo against different environment
.\demo-script.ps1 -BaseUrl "http://localhost:8080" -Detailed
```

## Expected Output

The demo will show:

1. ✅ **System Health Check** - Verify application is running
2. 🎯 **Sample Data Generation** - Create realistic test patterns
3. 📊 **Pattern Detection** - Display detected error patterns with priorities and confidence scores
4. 🔍 **Advanced Filtering** - Demonstrate filtering capabilities
5. 📋 **Detailed Analysis** - Show individual pattern analysis
6. 🔔 **Alert Management** - Display active alerts and their severity
7. 📊 **Dashboard Analytics** - Show system overview metrics
8. 🧠 **ML Insights** - Explain machine learning capabilities
9. 📋 **API Coverage** - List all available endpoints
10. 🎉 **Conclusion** - Summary of demonstrated features

## API Endpoints Demonstrated

- `GET /health` - System health check
- `POST /api/SampleData/generate` - Generate sample data
- `GET /api/Patterns` - List all error patterns
- `GET /api/Patterns/{id}` - Get specific pattern details
- `GET /api/Patterns?priority=High` - Filter by priority
- `GET /api/Patterns?type=Persistent` - Filter by type
- `GET /api/Patterns?minConfidence=0.85` - Filter by confidence
- `GET /api/Patterns?timeframe=72` - Filter by time range
- `GET /api/Alerts` - List all alerts
- `GET /api/Dashboard/overview` - System overview metrics

## Troubleshooting

### Application Not Running
```
❌ Health check - Error: Unable to connect to the remote server
```
**Solution**: Start the AutoFixer application:
```bash
cd AutoFixer
dotnet run --urls "http://localhost:5000"
```

### MongoDB Not Available
```
❌ Sample data generation - Error: MongoDB connection failed
```
**Solution**: Start MongoDB container:
```bash
docker start org-licensing-mongo-v5
```

### PowerShell Execution Policy
```
Execution of scripts is disabled on this system
```
**Solution**: Run with bypass:
```powershell
powershell -ExecutionPolicy Bypass -File "demo-script.ps1"
```

## Next Steps

After running the demo:

1. **Explore Swagger UI**: Visit `http://localhost:5000/api-docs`
2. **Integrate with Your Systems**: Use the REST API to integrate with your logging infrastructure
3. **Configure Notifications**: Set up Slack, Teams, or email alerts
4. **Customize ML Models**: Adjust clustering parameters for your specific error patterns
5. **Scale with Production Data**: Replace sample data with real error logs

## Demo Video Script

For presentation purposes, the demo follows this narrative:

1. **Opening**: "AutoFixer is an intelligent error pattern detection system that uses machine learning to identify, cluster, and alert on critical application errors."

2. **Health Check**: "First, we verify our system is healthy and all services are operational."

3. **Data Generation**: "We'll generate realistic sample data representing various error patterns your applications might encounter."

4. **Pattern Detection**: "Our ML engine analyzes error messages using TF-IDF and DBSCAN clustering to identify patterns with confidence scores."

5. **Advanced Filtering**: "You can filter patterns by priority, type, confidence level, and time range to focus on what matters most."

6. **Detailed Analysis**: "Each pattern provides detailed insights including affected services, occurrence rates, and user impact."

7. **Alert Management**: "The system generates real-time alerts with multiple severity levels for immediate notification."

8. **Analytics Dashboard**: "Comprehensive metrics provide a bird's-eye view of your system's error landscape."

9. **ML Insights**: "Behind the scenes, advanced machine learning algorithms continuously improve pattern detection and anomaly identification."

10. **Conclusion**: "AutoFixer transforms reactive error handling into proactive intelligent monitoring."

---

**AutoFixer** - Intelligent Error Pattern Detection & Alerting System  
*Powered by ML.NET, ASP.NET Core, and MongoDB*