# PowerShell script to test OnlyOffice Server Framework API endpoints
# Run this from PowerShell after starting the server

Write-Host "`n=== Testing OnlyOffice Server Framework API ===" -ForegroundColor Cyan

# Test 1: Test Controller
Write-Host "`n[TEST 1] GET /api/test" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "http://localhost:5142/api/test" -Method GET
    Write-Host "✅ Success!" -ForegroundColor Green
    $response | ConvertTo-Json
} catch {
    Write-Host "❌ Failed: $_" -ForegroundColor Red
}

# Test 2: Auth Status Endpoint
Write-Host "`n[TEST 2] GET /api/auth/status" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "http://localhost:5142/api/auth/status" -Method GET
    Write-Host "✅ Success!" -ForegroundColor Green
    $response | ConvertTo-Json
} catch {
    Write-Host "❌ Failed: $_" -ForegroundColor Red
}

# Test 3: CORS Headers (should include Access-Control-Allow-Origin)
Write-Host "`n[TEST 3] CORS Headers Check" -ForegroundColor Yellow
try {
    $headers = @{
        "Origin" = "http://localhost:4200"
    }
    $response = Invoke-WebRequest -Uri "http://localhost:5142/api/auth/status" -Method GET -Headers $headers
    Write-Host "✅ Response received" -ForegroundColor Green
    Write-Host "Access-Control-Allow-Origin: $($response.Headers['Access-Control-Allow-Origin'])"
    Write-Host "Access-Control-Allow-Credentials: $($response.Headers['Access-Control-Allow-Credentials'])"
} catch {
    Write-Host "❌ Failed: $_" -ForegroundColor Red
}

# Test 4: Check if database was created
Write-Host "`n[TEST 4] Database File Check" -ForegroundColor Yellow
$dbPath = ".\App_Data\onlyoffice.db"
if (Test-Path $dbPath) {
    $dbFile = Get-Item $dbPath
    Write-Host "✅ Database exists!" -ForegroundColor Green
    Write-Host "   Path: $($dbFile.FullName)"
    Write-Host "   Size: $($dbFile.Length) bytes"
    Write-Host "   Created: $($dbFile.CreationTime)"
} else {
    Write-Host "❌ Database not found at: $dbPath" -ForegroundColor Red
}

Write-Host "`n=== Tests Complete ===" -ForegroundColor Cyan
