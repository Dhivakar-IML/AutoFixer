# AutoFixer Quick Reference

## Demo Scripts Available

### 1. Basic Demo
```powershell
.\demo-script-clean.ps1
```
*Runs complete demo with sample data generation*

### 2. Detailed Demo  
```powershell
.\demo-script-clean.ps1 -Detailed
```
*Shows detailed API responses and pattern analysis*

### 3. Use Existing Data
```powershell
.\demo-script-clean.ps1 -SkipDataGeneration -Detailed
```
*Runs demo without generating new sample data*

### 4. Custom Environment
```powershell
.\demo-script-clean.ps1 -BaseUrl "http://localhost:8080"
```
*Runs demo against different base URL*

### 5. Batch File (Easiest)
```batch
run-demo.bat
```
*Double-click to run basic demo*

## Key Features Demonstrated

| Feature | Description | API Endpoint |
|---------|-------------|--------------|
| **Health Check** | System status verification | `GET /health` |
| **Sample Data** | Generate realistic test patterns | `POST /api/SampleData/generate` |
| **Pattern Detection** | List all detected error patterns | `GET /api/Patterns` |
| **Advanced Filtering** | Filter by priority, type, confidence, time | `GET /api/Patterns?filters` |
| **Pattern Details** | Deep-dive analysis of specific patterns | `GET /api/Patterns/{id}` |
| **Alert Management** | Real-time alert retrieval | `GET /api/Alerts` |
| **Dashboard Analytics** | System overview and metrics | `GET /api/Dashboard/overview` |

## Sample API Calls

### Get All Patterns
```powershell
Invoke-RestMethod -Uri "http://localhost:5000/api/Patterns" -Method GET
```

### Filter High Priority Patterns
```powershell
Invoke-RestMethod -Uri "http://localhost:5000/api/Patterns?priority=High" -Method GET
```

### Generate Sample Data
```powershell
Invoke-RestMethod -Uri "http://localhost:5000/api/SampleData/generate" -Method POST
```

### Get System Health
```powershell
Invoke-RestMethod -Uri "http://localhost:5000/health" -Method GET
```

## Machine Learning Features

- **TF-IDF Text Featurization**: Converts error messages into numerical features
- **DBSCAN Clustering**: Groups similar errors into patterns  
- **Cosine Similarity**: Measures pattern similarity for matching
- **Confidence Scoring**: Rates pattern reliability (0-100%)
- **Anomaly Detection**: Identifies unusual error patterns
- **Trend Analysis**: Tracks pattern evolution over time

## Pattern Priority Levels

| Level | Color | Description |
|-------|-------|-------------|
| **Critical (3)** | Red | System-breaking errors requiring immediate attention |
| **High (2)** | Yellow | Significant issues affecting user experience |
| **Medium (1)** | Cyan | Moderate issues that should be addressed |
| **Low (0)** | Green | Minor issues for routine maintenance |

## Pattern Status Types

| Status | Icon | Description |
|--------|------|-------------|
| **Active (0)** | [ACTIVE] | Currently occurring and needs attention |
| **Investigation Pending (1)** | [PENDING] | Requires investigation |
| **In Progress (2)** | [IN_PROGRESS] | Being actively worked on |
| **Resolved (3)** | [RESOLVED] | Fixed and closed |
| **Ignored (4)** | [IGNORED] | Acknowledged but not actionable |
| **Archived (5)** | [ARCHIVED] | Historical record |

## Swagger UI Access

**URL**: http://localhost:5000/api-docs

Interactive API documentation where you can:
- Test all endpoints directly
- View request/response schemas
- Generate code samples
- Explore API functionality

## Quick Troubleshooting

### Application Not Running
```bash
cd AutoFixer
dotnet run --urls "http://localhost:5000"
```

### MongoDB Not Available
```bash
docker start org-licensing-mongo-v5
```

### Clear Sample Data
```powershell
# Delete all test patterns (if needed)
Invoke-RestMethod -Uri "http://localhost:5000/api/Patterns/clear" -Method DELETE
```

## Demo Success Indicators

✅ **Health Check**: Returns 200 OK  
✅ **Sample Data**: Creates 4+ error patterns  
✅ **Pattern Retrieval**: Shows patterns with confidence scores  
✅ **Filtering**: Returns filtered results  
✅ **Pattern Details**: Shows individual pattern information  
✅ **Alerts**: Lists active system alerts  
✅ **Dashboard**: Displays system metrics  

## Production Integration

After demo success:

1. **Logging Integration**: Connect your application logs
2. **Notification Setup**: Configure Slack/Teams/Email alerts  
3. **Custom Patterns**: Define domain-specific error patterns
4. **Monitoring Setup**: Schedule pattern detection workflows
5. **Dashboard Access**: Set up team access to analytics

---

**AutoFixer Demo Package v1.0**  
*Complete intelligent error pattern detection demonstration*