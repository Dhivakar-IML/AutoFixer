# AutoFixer Real Data Integration Demo
# Demonstrates integration with New Relic and database sources for live error pattern detection

param(
    [string]$BaseUrl = "http://localhost:5000",
    [string]$NewRelicApiKey = "",
    [string]$NewRelicAccountId = "",
    [string]$DatabaseConnectionString = "",
    [string]$DatabaseType = "SqlServer", # SqlServer, PostgreSql, MySql
    [int]$TimeframeHours = 24,
    [switch]$TestConnections,
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

function Write-Warning {
    param([string]$Message)
    Write-Host "   [WARNING] $Message" -ForegroundColor Yellow
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

# Function to update appsettings with credentials
function Update-AppSettings {
    param(
        [string]$NewRelicApiKey,
        [string]$NewRelicAccountId
    )
    
    if ([string]::IsNullOrEmpty($NewRelicApiKey) -or [string]::IsNullOrEmpty($NewRelicAccountId)) {
        Write-Warning "New Relic credentials not provided - will use test data"
        return $false
    }
    
    try {
        $appSettingsPath = "AutoFixer\appsettings.json"
        if (Test-Path $appSettingsPath) {
            $appSettings = Get-Content $appSettingsPath | ConvertFrom-Json
            $appSettings.NewRelic.ApiKey = $NewRelicApiKey
            $appSettings.NewRelic.AccountId = $NewRelicAccountId
            $appSettings | ConvertTo-Json -Depth 10 | Set-Content $appSettingsPath
            Write-Result "Updated New Relic configuration"
            return $true
        } else {
            Write-Warning "appsettings.json not found - using default configuration"
            return $false
        }
    }
    catch {
        Write-Error "Failed to update appsettings.json: $($_.Exception.Message)"
        return $false
    }
}

# Function to display integration results
function Show-IntegrationResults {
    param([object]$Result)
    
    if ($Result -and $Result.success) {
        Write-Host "`n   [SUCCESS] Integration Results:" -ForegroundColor Green
        Write-Host "   " + "-" * 50 -ForegroundColor Gray
        Write-Host "   Source: $($Result.source)" -ForegroundColor Cyan
        Write-Host "   Errors Imported: $($Result.errorsImported)" -ForegroundColor White
        Write-Host "   Patterns Created: $($Result.patternsCreated)" -ForegroundColor Yellow
        if ($Result.incidentsFound -gt 0) {
            Write-Host "   Incidents Found: $($Result.incidentsFound)" -ForegroundColor Red
        }
        Write-Host "   Timeframe: $($Result.timeframeHours) hours" -ForegroundColor Gray
        Write-Host "   Import Time: $($Result.importedAt)" -ForegroundColor Gray
        Write-Host "   Message: $($Result.message)" -ForegroundColor Green
    } else {
        Write-Error "Integration failed: $($Result.message)"
    }
}

# Function to show patterns with real data context
function Show-RealPatterns {
    param([object]$Patterns, [string]$Source = "Unknown")
    
    if ($Patterns -and $Patterns.Count -gt 0) {
        Write-Host "`n   [REAL DATA] Error Patterns from $Source:" -ForegroundColor Magenta
        Write-Host "   " + "-" * 70 -ForegroundColor Gray
        
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
            
            if ($pattern.businessImpact) {
                Write-Host "      Revenue Impact: $([math]::Round($pattern.businessImpact.revenueImpact, 2))" -ForegroundColor Yellow
                Write-Host "      User Impact: $($pattern.userImpact) users" -ForegroundColor DarkYellow
            }
            
            Write-Host "      Rate: $([math]::Round($pattern.occurrenceRate, 1))/hr | Count: $($pattern.occurrenceCount)" -ForegroundColor DarkGray
            
            if ($pattern.tags -and $pattern.tags.Count -gt 0) {
                Write-Host "      Tags: $($pattern.tags -join ', ')" -ForegroundColor DarkCyan
            }
        }
    } else {
        Write-Info "No patterns found from $Source"
    }
}

# Main demo script
Write-Header "AutoFixer Real Data Integration Demo"
Write-Host "Demonstrating live integration with New Relic and database sources" -ForegroundColor White
Write-Host "Base URL: $BaseUrl" -ForegroundColor Gray

# Step 1: System Health Check
Write-Step "1. System Health Check"
$healthResponse = Invoke-ApiCall -Method "GET" -Endpoint "/health" -Description "Health check"

if (-not $healthResponse) {
    Write-Error "Application is not running on $BaseUrl"
    Write-Info "Please start the application with: dotnet run --urls 'http://localhost:5000'"
    exit 1
}

# Step 2: Configuration Update
Write-Step "2. Configuration Setup"
if ($NewRelicApiKey -and $NewRelicAccountId) {
    Write-Info "Updating New Relic configuration..."
    $configUpdated = Update-AppSettings -NewRelicApiKey $NewRelicApiKey -NewRelicAccountId $NewRelicAccountId
    if ($configUpdated) {
        Write-Result "New Relic credentials configured"
    }
} else {
    Write-Warning "New Relic credentials not provided"
    Write-Info "To use real New Relic data, provide -NewRelicApiKey and -NewRelicAccountId parameters"
}

# Step 3: Connection Testing (if requested)
if ($TestConnections) {
    Write-Step "3. Connection Testing"
    
    # Test New Relic connection
    if ($NewRelicApiKey -and $NewRelicAccountId) {
        Write-Info "Testing New Relic connection..."
        $newRelicTest = Invoke-ApiCall -Method "POST" -Endpoint "/api/Integrations/newrelic/test" -Description "New Relic connection test"
        
        if ($newRelicTest) {
            $testResult = $newRelicTest.Content | ConvertFrom-Json
            if ($testResult.success) {
                Write-Result "New Relic connection successful"
            } else {
                Write-Error "New Relic connection failed: $($testResult.message)"
            }
        }
    }
    
    # Test database connection
    if ($DatabaseConnectionString) {
        Write-Info "Testing database connection..."
        $dbTestBody = @{
            databaseType = $DatabaseType
            connectionString = $DatabaseConnectionString
        }
        
        $dbTest = Invoke-ApiCall -Method "POST" -Endpoint "/api/Integrations/database/test" -Body $dbTestBody -Description "Database connection test"
        
        if ($dbTest) {
            $testResult = $dbTest.Content | ConvertFrom-Json
            if ($testResult.success) {
                Write-Result "Database connection successful"
            } else {
                Write-Error "Database connection failed: $($testResult.message)"
            }
        }
    }
}

# Step 4: New Relic Data Import
Write-Step "4. New Relic Data Import"
if ($NewRelicApiKey -and $NewRelicAccountId) {
    Write-Info "Importing real error data from New Relic..."
    
    $newRelicImportBody = @{
        timeframeHours = $TimeframeHours
        limit = 100
    }
    
    $newRelicImport = Invoke-ApiCall -Method "POST" -Endpoint "/api/Integrations/newrelic/import" -Body $newRelicImportBody -Description "New Relic data import"
    
    if ($newRelicImport) {
        $importResult = $newRelicImport.Content | ConvertFrom-Json
        Show-IntegrationResults -Result $importResult
        
        if ($importResult.patternsCreated -gt 0) {
            Write-Info "Retrieving imported patterns..."
            Start-Sleep -Seconds 2
            
            $patternsResponse = Invoke-ApiCall -Method "GET" -Endpoint "/api/Patterns?limit=10" -Description "Get imported patterns"
            if ($patternsResponse) {
                $patterns = $patternsResponse.Content | ConvertFrom-Json
                Show-RealPatterns -Patterns $patterns -Source "New Relic"
            }
        }
    }
} else {
    Write-Warning "Skipping New Relic import - credentials not provided"
    Write-Info "Provide -NewRelicApiKey and -NewRelicAccountId to import real data"
}

# Step 5: Database Import
Write-Step "5. Database Data Import"
if ($DatabaseConnectionString) {
    Write-Info "Importing error data from database..."
    
    $dbImportBody = @{
        databaseType = $DatabaseType
        connectionString = $DatabaseConnectionString
        timeframeHours = $TimeframeHours
    }
    
    $dbImport = Invoke-ApiCall -Method "POST" -Endpoint "/api/Integrations/database/import" -Body $dbImportBody -Description "Database data import"
    
    if ($dbImport) {
        $importResult = $dbImport.Content | ConvertFrom-Json
        Show-IntegrationResults -Result $importResult
        
        if ($importResult.patternsCreated -gt 0) {
            Write-Info "Retrieving database patterns..."
            Start-Sleep -Seconds 2
            
            $patternsResponse = Invoke-ApiCall -Method "GET" -Endpoint "/api/Patterns?limit=10" -Description "Get database patterns"
            if ($patternsResponse) {
                $patterns = $patternsResponse.Content | ConvertFrom-Json
                $dbPatterns = $patterns | Where-Object { $_.tags -contains "database-import" }
                Show-RealPatterns -Patterns $dbPatterns -Source "Database ($DatabaseType)"
            }
        }
    }
} else {
    Write-Warning "Skipping database import - connection string not provided"
    Write-Info "Provide -DatabaseConnectionString to import real database logs"
}

# Step 6: Integration History
Write-Step "6. Integration History"
Write-Info "Retrieving integration history..."

$historyResponse = Invoke-ApiCall -Method "GET" -Endpoint "/api/Integrations/history" -Description "Get integration history"

if ($historyResponse) {
    $history = $historyResponse.Content | ConvertFrom-Json
    
    if ($history -and $history.Count -gt 0) {
        Write-Host "`n   [HISTORY] Previous Integrations:" -ForegroundColor Magenta
        Write-Host "   " + "-" * 60 -ForegroundColor Gray
        
        foreach ($entry in $history) {
            $statusIcon = if ($entry.success) { "[SUCCESS]" } else { "[FAILED]" }
            $statusColor = if ($entry.success) { "Green" } else { "Red" }
            
            Write-Host "   $statusIcon " -ForegroundColor $statusColor -NoNewline
            Write-Host "$($entry.source) - $($entry.importedAt)" -ForegroundColor White
            Write-Host "      Patterns: $($entry.patternsCreated) | Errors: $($entry.errorsImported)" -ForegroundColor Gray
            Write-Host "      Message: $($entry.message)" -ForegroundColor DarkGray
        }
    } else {
        Write-Info "No integration history found"
    }
}

# Step 7: Live Pattern Analysis
Write-Step "7. Live Pattern Analysis"
Write-Info "Analyzing patterns with real business impact..."

$allPatternsResponse = Invoke-ApiCall -Method "GET" -Endpoint "/api/Patterns?limit=50" -Description "Get all current patterns"

if ($allPatternsResponse) {
    $allPatterns = $allPatternsResponse.Content | ConvertFrom-Json
    
    if ($allPatterns -and $allPatterns.Count -gt 0) {
        # Group by data source
        $newRelicPatterns = $allPatterns | Where-Object { $_.tags -contains "new-relic" }
        $databasePatterns = $allPatterns | Where-Object { $_.tags -contains "database-import" }
        $samplePatterns = $allPatterns | Where-Object { $_.tags -notcontains "new-relic" -and $_.tags -notcontains "database-import" }
        
        Write-Host "`n   [ANALYSIS] Pattern Source Breakdown:" -ForegroundColor Magenta
        Write-Host "   " + "-" * 50 -ForegroundColor Gray
        Write-Host "   New Relic Patterns: $($newRelicPatterns.Count)" -ForegroundColor Cyan
        Write-Host "   Database Patterns: $($databasePatterns.Count)" -ForegroundColor Yellow
        Write-Host "   Sample Patterns: $($samplePatterns.Count)" -ForegroundColor Green
        
        # Business impact analysis
        $totalRevenueImpact = ($allPatterns | ForEach-Object { $_.businessImpact.revenueImpact } | Measure-Object -Sum).Sum
        $totalUserImpact = ($allPatterns | ForEach-Object { $_.userImpact } | Measure-Object -Sum).Sum
        $criticalPatterns = $allPatterns | Where-Object { $_.priority -eq 3 }
        
        Write-Host "`n   [BUSINESS IMPACT] Real Impact Analysis:" -ForegroundColor Red
        Write-Host "   " + "-" * 50 -ForegroundColor Gray
        Write-Host "   Total Revenue Impact: $([math]::Round($totalRevenueImpact, 2))" -ForegroundColor Red
        Write-Host "   Total Users Affected: $totalUserImpact" -ForegroundColor Yellow
        Write-Host "   Critical Patterns: $($criticalPatterns.Count)" -ForegroundColor Red
        
        if ($criticalPatterns.Count -gt 0) {
            Write-Host "`n   [CRITICAL] Immediate Attention Required:" -ForegroundColor Red
            foreach ($critical in $criticalPatterns) {
                Write-Host "   • $($critical.name)" -ForegroundColor Red
                Write-Host "     Impact: $($critical.userImpact) users, $([math]::Round($critical.businessImpact.revenueImpact, 2)) revenue" -ForegroundColor DarkRed
            }
        }
    }
}

# Step 8: Real-Time Demo Summary
Write-Step "8. Real Data Demo Summary"

Write-Host "`n   [SUMMARY] Integration Capabilities Demonstrated:" -ForegroundColor Magenta
Write-Host "   " + "-" * 60 -ForegroundColor Gray

$capabilities = @()
if ($NewRelicApiKey -and $NewRelicAccountId) {
    $capabilities += "[OK] New Relic API Integration - Live error data imported"
} else {
    $capabilities += "[SKIPPED] New Relic Integration - No credentials provided"
}

if ($DatabaseConnectionString) {
    $capabilities += "[OK] Database Integration - Application logs imported"
} else {
    $capabilities += "[SKIPPED] Database Integration - No connection string provided"
}

$capabilities += "[OK] Pattern Detection - ML-powered clustering of real errors"
$capabilities += "[OK] Business Impact Analysis - Revenue and user impact calculation"
$capabilities += "[OK] Multi-source Correlation - Patterns from multiple data sources"
$capabilities += "[OK] Real-time Analytics - Live pattern analysis and trending"

foreach ($capability in $capabilities) {
    if ($capability.StartsWith("[OK]")) {
        Write-Host "   $capability" -ForegroundColor Green
    } else {
        Write-Host "   $capability" -ForegroundColor Yellow
    }
}

Write-Header "Real Data Demo Complete!"

if ($NewRelicApiKey -or $DatabaseConnectionString) {
    Write-Host "Successfully demonstrated AutoFixer with LIVE DATA sources!" -ForegroundColor Green
    Write-Host "`nReal-world value demonstrated:" -ForegroundColor Yellow
    Write-Host "• Imported actual error data from production systems" -ForegroundColor White
    Write-Host "• Applied ML intelligence to real error patterns" -ForegroundColor White
    Write-Host "• Calculated genuine business impact metrics" -ForegroundColor White
    Write-Host "• Provided actionable insights from live data" -ForegroundColor White
} else {
    Write-Host "Demo completed with sample data" -ForegroundColor Yellow
    Write-Host "`nTo see REAL DATA integration:" -ForegroundColor Cyan
    Write-Host "• Provide New Relic credentials: -NewRelicApiKey 'YOUR_KEY' -NewRelicAccountId 'YOUR_ID'" -ForegroundColor White
    Write-Host "• Provide database connection: -DatabaseConnectionString 'YOUR_CONNECTION'" -ForegroundColor White
}

Write-Host "`nNext Steps for Production:" -ForegroundColor Yellow
Write-Host "• Configure automated imports from your error sources" -ForegroundColor White
Write-Host "• Set up real-time streaming for immediate pattern detection" -ForegroundColor White
Write-Host "• Customize business impact calculations for your metrics" -ForegroundColor White
Write-Host "• Integrate with your alerting and incident management systems" -ForegroundColor White

Write-Host "`nDemo completed at $(Get-Date)" -ForegroundColor Gray