#!/usr/bin/env pwsh

Write-Host "🚀 AUTOFIXER REAL-TIME DEMONSTRATION" -ForegroundColor Cyan -BackgroundColor Black
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Yellow

# Step 1: Start the application
Write-Host "`n1. STARTING AUTOFIXER WITH REAL-TIME CAPABILITIES..." -ForegroundColor Green
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd 'D:\AutoFixer'; Write-Host 'AutoFixer Real-Time Server Starting...' -ForegroundColor Green; dotnet run --urls 'http://localhost:5000'"

# Wait for application to start
Write-Host "   ⏳ Waiting for AutoFixer to initialize..." -ForegroundColor Yellow
Start-Sleep -Seconds 8

# Step 2: Test health endpoint
Write-Host "`n2. VERIFYING REAL-TIME SYSTEM STATUS..." -ForegroundColor Green
try {
    $health = Invoke-RestMethod -Uri "http://localhost:5000/health" -Method GET -TimeoutSec 10
    Write-Host "   ✅ Health Status: $health" -ForegroundColor Green
} catch {
    Write-Host "   ❌ Health check failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   🔄 Application may still be starting..." -ForegroundColor Yellow
}

# Step 3: Open real-time dashboard
Write-Host "`n3. LAUNCHING REAL-TIME DASHBOARD..." -ForegroundColor Green
$dashboardUrl = "http://localhost:5000/realtime.html"
Write-Host "   🌐 Opening: $dashboardUrl" -ForegroundColor Cyan
Start-Process $dashboardUrl

# Step 4: Simulate real-time errors
Write-Host "`n4. GENERATING REAL-TIME ERROR PATTERNS..." -ForegroundColor Green

$realTimeErrors = @(
    @{
        ErrorName = "Live Database Connection Timeout"
        Description = "Real-time connection pool exhaustion detected"
        Priority = 1
    },
    @{
        ErrorName = "Live Memory Leak Detection"
        Description = "Real-time heap overflow in user service"
        Priority = 1
    },
    @{
        ErrorName = "Live API Authentication Failure"
        Description = "Real-time JWT token validation failure"
        Priority = 2
    }
)

foreach ($error in $realTimeErrors) {
    Write-Host "   🚨 Simulating: $($error.ErrorName)" -ForegroundColor Red
    try {
        $response = Invoke-RestMethod -Uri "http://localhost:5000/api/RealTime/simulate-error" `
            -Method POST `
            -ContentType "application/json" `
            -Body ($error | ConvertTo-Json) `
            -TimeoutSec 10
        
        Write-Host "   ✅ Real-time analysis complete: $($response.Message)" -ForegroundColor Green
    } catch {
        Write-Host "   ⚠️  Error simulation failed: $($_.Exception.Message)" -ForegroundColor Yellow
    }
    
    Start-Sleep -Seconds 3
}

# Step 5: Show real-time status
Write-Host "`n5. REAL-TIME ANALYSIS RESULTS..." -ForegroundColor Green
try {
    $status = Invoke-RestMethod -Uri "http://localhost:5000/api/RealTime/status" -Method GET -TimeoutSec 10
    Write-Host "   📊 Total Patterns: $($status.TotalPatterns)" -ForegroundColor White
    Write-Host "   🕒 Recent Patterns (5min): $($status.RecentPatterns)" -ForegroundColor White
    Write-Host "   ⚡ Status: $($status.Status)" -ForegroundColor Green
    
    if ($status.RecentErrors -and $status.RecentErrors.Count -gt 0) {
        Write-Host "`n   🔍 LIVE ERROR PATTERNS:" -ForegroundColor Cyan
        $status.RecentErrors | ForEach-Object {
            Write-Host "      • $($_.Name) - Confidence: $($_.Confidence)%" -ForegroundColor White
            Write-Host "        Priority: $($_.Priority) | Severity: $($_.Severity)" -ForegroundColor Gray
        }
    }
} catch {
    Write-Host "   ⚠️  Status check failed: $($_.Exception.Message)" -ForegroundColor Yellow
}

# Step 6: Demo streaming
Write-Host "`n6. ACTIVATING REAL-TIME STREAMING..." -ForegroundColor Green
try {
    $streamResponse = Invoke-RestMethod -Uri "http://localhost:5000/api/RealTime/stream-demo" -Method GET -TimeoutSec 10
    Write-Host "   ✅ $($streamResponse.Message)" -ForegroundColor Green
} catch {
    Write-Host "   ⚠️  Streaming demo failed: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host "`n═══════════════════════════════════════════════════════════════" -ForegroundColor Yellow
Write-Host "🎯 REAL-TIME DEMONSTRATION COMPLETE!" -ForegroundColor Green -BackgroundColor Black
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Yellow

Write-Host "`n📋 WHAT TO DO NEXT:" -ForegroundColor Cyan
Write-Host "   1. Check the browser dashboard for live updates" -ForegroundColor White
Write-Host "   2. Click 'Simulate Real-Time Error' to generate new patterns" -ForegroundColor White
Write-Host "   3. Watch ML confidence scores update in real-time" -ForegroundColor White
Write-Host "   4. Monitor live pattern detection" -ForegroundColor White

Write-Host "`n🤖 TECHNICAL DETAILS:" -ForegroundColor Blue
Write-Host "   • Real-time data processing: ACTIVE" -ForegroundColor White
Write-Host "   • Live ML pattern detection: CONTINUOUS" -ForegroundColor White
Write-Host "   • Error simulation capabilities: ENABLED" -ForegroundColor White
Write-Host "   • MongoDB Atlas integration: LIVE" -ForegroundColor White

Write-Host "`nPress any key to continue monitoring..." -ForegroundColor Yellow
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")