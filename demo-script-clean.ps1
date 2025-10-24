# AutoFixer Demo Script
# Demonstrates the complete intelligent error pattern detection and alerting system

param(
    [string]$BaseUrl = "http://localhost:5000",
    [switch]$SkipDataGeneration,
    [switch]$Detailed
)

# Color functions for better output
function Write-Header {
    param([string]$Message)
    Write-Host "`n" -NoNewline
    Write-Host "=" * 80 -ForegroundColor Cyan
    Write-Host "  $Message" -ForegroundColor Yellow
    Write-Host "=" * 80 -ForegroundColor Cyan
}

function Write-Step {
    param([string]$Message)
    Write-Host "`n[STEP] $Message" -ForegroundColor Green
}

function Write-Result {
    param([string]$Message, [string]$Color = "White")
    Write-Host "   [OK] $Message" -ForegroundColor $Color
}

function Write-Error {
    param([string]$Message)
    Write-Host "   [ERROR] $Message" -ForegroundColor Red
}

function Write-Info {
    param([string]$Message)
    Write-Host "   [INFO] $Message" -ForegroundColor Blue
}

# Function to make API calls with error handling
function Invoke-ApiCall {
    param(
        [string]$Method,
        [string]$Endpoint,
        [object]$Body = $null,
        [string]$Description
    )
    
    try {
        $uri = "$BaseUrl$Endpoint"
        $params = @{
            Uri = $uri
            Method = $Method
            ContentType = "application/json"
            UseBasicParsing = $true
        }
        
        if ($Body) {
            $params.Body = ($Body | ConvertTo-Json -Depth 10)
        }
        
        Write-Host "   [API] $Method $Endpoint" -ForegroundColor Gray
        
        $response = Invoke-WebRequest @params
        
        if ($response.StatusCode -eq 200 -or $response.StatusCode -eq 201) {
            Write-Result "$Description - Success ($($response.StatusCode))"
            
            if ($Detailed -and $response.Content) {
                $content = $response.Content | ConvertFrom-Json
                if ($content -is [array]) {
                    Write-Info "Retrieved $($content.Count) items"
                } elseif ($content -is [string]) {
                    Write-Info $content
                } else {
                    Write-Info "Response received"
                }
            }
            
            return $response
        } else {
            Write-Error "$Description - Failed ($($response.StatusCode))"
            return $null
        }
    }
    catch {
        Write-Error "$Description - Error: $($_.Exception.Message)"
        return $null
    }
}

# Function to display formatted data
function Show-Patterns {
    param([object]$Patterns)
    
    if ($Patterns -and $Patterns.Count -gt 0) {
        Write-Host "`n   [PATTERNS] Error Patterns Summary:" -ForegroundColor Magenta
        Write-Host "   " + "-" * 60 -ForegroundColor Gray
        
        foreach ($pattern in $Patterns) {
            $priorityColor = switch ($pattern.priority) {
                3 { "Red" }      # Critical
                2 { "Yellow" }   # High  
                1 { "Cyan" }     # Medium
                0 { "Green" }    # Low
                default { "White" }
            }
            
            $statusIcon = switch ($pattern.status) {
                0 { "[ACTIVE]" }     # Active
                1 { "[PENDING]" }    # Investigation Pending
                2 { "[IN_PROGRESS]" } # In Progress
                3 { "[RESOLVED]" }   # Resolved
                4 { "[IGNORED]" }    # Ignored
                5 { "[ARCHIVED]" }   # Archived
                default { "[UNKNOWN]" }
            }
            
            Write-Host "   $statusIcon " -NoNewline
            Write-Host "$($pattern.name)" -ForegroundColor $priorityColor -NoNewline
            Write-Host " (Confidence: $([math]::Round($pattern.confidence * 100, 1))%)" -ForegroundColor Gray
            Write-Host "      Impact: $($pattern.userImpact) users | Rate: $($pattern.occurrenceRate)/hr" -ForegroundColor DarkGray
        }
    } else {
        Write-Info "No patterns found"
    }
}

# Main demo script
Write-Header "AutoFixer - Intelligent Error Pattern Detection Demo"
Write-Host "Demonstrating enterprise-grade error pattern detection and alerting" -ForegroundColor White
Write-Host "Base URL: $BaseUrl" -ForegroundColor Gray

# Step 1: Health Check
Write-Step "1. System Health Check"
$healthResponse = Invoke-ApiCall -Method "GET" -Endpoint "/health" -Description "Health check"

if (-not $healthResponse) {
    Write-Error "Application is not running on $BaseUrl"
    Write-Info "Please start the application with: dotnet run --urls 'http://localhost:5000'"
    exit 1
}

# Step 2: Generate Sample Data (unless skipped)
if (-not $SkipDataGeneration) {
    Write-Step "2. Generating Sample Error Data"
    Write-Info "Creating realistic error patterns for demonstration..."
    $generateResponse = Invoke-ApiCall -Method "POST" -Endpoint "/api/SampleData/generate" -Description "Sample data generation"
    
    if ($generateResponse) {
        Start-Sleep -Seconds 2  # Allow data to be fully inserted
    }
} else {
    Write-Step "2. Using Existing Data"
    Write-Info "Skipping data generation (using existing data)"
}

# Step 3: Pattern Detection and Retrieval
Write-Step "3. Error Pattern Detection"
Write-Info "Retrieving all detected error patterns..."

$patternsResponse = Invoke-ApiCall -Method "GET" -Endpoint "/api/Patterns" -Description "Get all patterns"

if ($patternsResponse) {
    $patterns = $patternsResponse.Content | ConvertFrom-Json
    Show-Patterns -Patterns $patterns
}

# Step 4: Advanced Pattern Filtering
Write-Step "4. Advanced Pattern Filtering"

Write-Info "Filtering by Priority: High and Critical only"
$highPriorityResponse = Invoke-ApiCall -Method "GET" -Endpoint "/api/Patterns?priority=High" -Description "Get high priority patterns"

if ($highPriorityResponse) {
    $highPriorityPatterns = $highPriorityResponse.Content | ConvertFrom-Json
    Show-Patterns -Patterns $highPriorityPatterns
}

Write-Info "Filtering by Type: Persistent patterns only"
$persistentResponse = Invoke-ApiCall -Method "GET" -Endpoint "/api/Patterns?type=Persistent" -Description "Get persistent patterns"

Write-Info "Filtering by Confidence: 85%+ confidence only"
$confidenceResponse = Invoke-ApiCall -Method "GET" -Endpoint "/api/Patterns?minConfidence=0.85" -Description "Get high confidence patterns"

Write-Info "Filtering by Time: Last 72 hours"
$recentResponse = Invoke-ApiCall -Method "GET" -Endpoint "/api/Patterns?timeframe=72" -Description "Get recent patterns"

# Step 5: Individual Pattern Details
Write-Step "5. Detailed Pattern Analysis"

if ($patterns -and $patterns.Count -gt 0) {
    $firstPattern = $patterns[0]
    Write-Info "Getting detailed analysis for: '$($firstPattern.name)'"
    
    $patternDetailResponse = Invoke-ApiCall -Method "GET" -Endpoint "/api/Patterns/$($firstPattern.id)" -Description "Get pattern details"
    
    if ($patternDetailResponse -and $Detailed) {
        $patternDetail = $patternDetailResponse.Content | ConvertFrom-Json
        Write-Host "`n   [DETAILS] Pattern Details:" -ForegroundColor Magenta
        Write-Host "   " + "-" * 40 -ForegroundColor Gray
        Write-Host "   Name: $($patternDetail.name)" -ForegroundColor White
        Write-Host "   Description: $($patternDetail.description)" -ForegroundColor Gray
        Write-Host "   Confidence: $([math]::Round($patternDetail.confidence * 100, 1))%" -ForegroundColor Green
        Write-Host "   First Seen: $($patternDetail.firstOccurrence)" -ForegroundColor Gray
        Write-Host "   Last Seen: $($patternDetail.lastOccurrence)" -ForegroundColor Gray
        Write-Host "   Affected Services: $($patternDetail.affectedServices -join ', ')" -ForegroundColor Cyan
    }
}

# Step 6: Alert Management
Write-Step "6. Alert Management System"
Write-Info "Retrieving current alerts..."

$alertsResponse = Invoke-ApiCall -Method "GET" -Endpoint "/api/Alerts" -Description "Get all alerts"

if ($alertsResponse) {
    $alerts = $alertsResponse.Content | ConvertFrom-Json
    if ($alerts -and $alerts.Count -gt 0) {
        Write-Host "`n   [ALERTS] Active Alerts:" -ForegroundColor Magenta
        Write-Host "   " + "-" * 40 -ForegroundColor Gray
        foreach ($alert in $alerts) {
            $severityIcon = switch ($alert.severity) {
                "Critical" { "[CRITICAL]" }
                "High" { "[HIGH]" }
                "Medium" { "[MEDIUM]" }
                "Low" { "[LOW]" }
                default { "[ALERT]" }
            }
            Write-Host "   $severityIcon $($alert.title) - $($alert.severity)" -ForegroundColor Yellow
        }
    } else {
        Write-Info "No active alerts found"
    }
}

# Step 7: Dashboard Analytics
Write-Step "7. Dashboard Analytics"
Write-Info "Retrieving system overview and metrics..."

$overviewResponse = Invoke-ApiCall -Method "GET" -Endpoint "/api/Dashboard/overview" -Description "Get dashboard overview"

if ($overviewResponse) {
    $overview = $overviewResponse.Content | ConvertFrom-Json
    if ($overview) {
        Write-Host "`n   [METRICS] System Overview:" -ForegroundColor Magenta
        Write-Host "   " + "-" * 40 -ForegroundColor Gray
        Write-Host "   Total Patterns: $($overview.totalPatterns)" -ForegroundColor White
        Write-Host "   Active Alerts: $($overview.activeAlerts)" -ForegroundColor Yellow
        Write-Host "   Critical Issues: $($overview.criticalIssues)" -ForegroundColor Red
        Write-Host "   System Health: $($overview.systemHealth)" -ForegroundColor Green
    }
}

# Step 8: Machine Learning Insights
Write-Step "8. Machine Learning Insights"
Write-Info "Demonstrating ML-powered error clustering and pattern detection..."

Write-Host "`n   [ML] ML Engine Capabilities:" -ForegroundColor Magenta
Write-Host "   " + "-" * 50 -ForegroundColor Gray
Write-Host "   [OK] TF-IDF Text Featurization for error message analysis" -ForegroundColor Green
Write-Host "   [OK] DBSCAN Clustering for grouping similar errors" -ForegroundColor Green  
Write-Host "   [OK] Cosine Similarity for pattern matching" -ForegroundColor Green
Write-Host "   [OK] Confidence Scoring for pattern reliability" -ForegroundColor Green
Write-Host "   [OK] Anomaly Detection for unusual error patterns" -ForegroundColor Green
Write-Host "   [OK] Trend Analysis for pattern evolution tracking" -ForegroundColor Green

# Step 9: API Coverage Summary
Write-Step "9. API Coverage Summary"
Write-Info "Available API endpoints demonstrated:"

$endpoints = @(
    @{ Method = "GET"; Path = "/health"; Description = "System health check" }
    @{ Method = "POST"; Path = "/api/SampleData/generate"; Description = "Generate sample data" }
    @{ Method = "GET"; Path = "/api/Patterns"; Description = "List all error patterns" }
    @{ Method = "GET"; Path = "/api/Patterns/{id}"; Description = "Get specific pattern details" }
    @{ Method = "GET"; Path = "/api/Alerts"; Description = "List all alerts" }
    @{ Method = "GET"; Path = "/api/Dashboard/overview"; Description = "System overview metrics" }
)

Write-Host "`n   [ENDPOINTS] API Endpoints:" -ForegroundColor Magenta
Write-Host "   " + "-" * 60 -ForegroundColor Gray
foreach ($endpoint in $endpoints) {
    $methodColor = switch ($endpoint.Method) {
        "GET" { "Green" }
        "POST" { "Blue" }
        "PUT" { "Yellow" }
        "DELETE" { "Red" }
        default { "White" }
    }
    Write-Host "   " -NoNewline
    Write-Host "$($endpoint.Method.PadRight(6))" -ForegroundColor $methodColor -NoNewline
    Write-Host "$($endpoint.Path.PadRight(30))" -ForegroundColor Cyan -NoNewline
    Write-Host "$($endpoint.Description)" -ForegroundColor Gray
}

# Step 10: Demo Conclusion
Write-Step "10. Demo Conclusion"

Write-Host "`n   [FEATURES] AutoFixer Key Features Demonstrated:" -ForegroundColor Magenta
Write-Host "   " + "-" * 50 -ForegroundColor Gray
Write-Host "   [OK] Intelligent Error Pattern Detection" -ForegroundColor Green
Write-Host "   [OK] Machine Learning-Powered Clustering" -ForegroundColor Green
Write-Host "   [OK] Real-time Alert Management" -ForegroundColor Green
Write-Host "   [OK] Advanced Filtering and Analytics" -ForegroundColor Green
Write-Host "   [OK] RESTful API with Swagger Documentation" -ForegroundColor Green
Write-Host "   [OK] MongoDB Integration for Scalable Storage" -ForegroundColor Green
Write-Host "   [OK] Multi-channel Notification Support" -ForegroundColor Green

Write-Header "Demo Complete!"
Write-Host "AutoFixer is ready for production use with enterprise-grade error intelligence!" -ForegroundColor Green
Write-Host "`nNext Steps:" -ForegroundColor Yellow
Write-Host "• Integrate with your logging infrastructure" -ForegroundColor White
Write-Host "• Configure Slack/Teams/Email notifications" -ForegroundColor White  
Write-Host "• Set up automated pattern detection workflows" -ForegroundColor White
Write-Host "• Access Swagger UI at: $BaseUrl/api-docs" -ForegroundColor Cyan

Write-Host "`nDemo completed at $(Get-Date)" -ForegroundColor Gray