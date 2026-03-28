param(
    [string]$ApiBaseUrl = "http://localhost:5001",
    [string]$RestBaseUrl = "http://localhost:5002",
    [string]$OutputDir = "C:\Projects\JB2026\openspec\changes\phase-4-backend-and-api-migration\snapshots",
    [switch]$IncludeNonGet
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function ConvertTo-Base64Url {
    param([byte[]]$Bytes)
    return [Convert]::ToBase64String($Bytes).TrimEnd('=') -replace '\+', '-' -replace '/', '_'
}

function New-LegacyToken {
    param(
        [Parameter(Mandatory=$true)][string]$Secret,
        [Parameter(Mandatory=$true)][string]$Subject,
        [int]$ExpiryMinutes = 60
    )

    $headerJson = '{"alg":"HS256","typ":"JWT"}'
    $exp = [DateTimeOffset]::UtcNow.AddMinutes($ExpiryMinutes).ToUnixTimeSeconds()
    $payloadObj = @{ 
        "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name" = $Subject
        exp = $exp
    }
    $payloadJson = ($payloadObj | ConvertTo-Json -Compress)

    $header = ConvertTo-Base64Url -Bytes ([Text.Encoding]::UTF8.GetBytes($headerJson))
    $payload = ConvertTo-Base64Url -Bytes ([Text.Encoding]::UTF8.GetBytes($payloadJson))
    $unsigned = "$header.$payload"

    $keyBytes = [Convert]::FromBase64String($Secret)
    $hmac = [System.Security.Cryptography.HMACSHA256]::new($keyBytes)
    try {
        $signatureBytes = $hmac.ComputeHash([Text.Encoding]::UTF8.GetBytes($unsigned))
    }
    finally {
        $hmac.Dispose()
    }

    $signature = ConvertTo-Base64Url -Bytes $signatureBytes
    return "$unsigned.$signature"
}

function Ensure-Dir {
    param([string]$Path)
    if (-not (Test-Path $Path)) {
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
    }
}

function Save-Snapshot {
    param(
        [Parameter(Mandatory=$true)][string]$Name,
        [Parameter(Mandatory=$true)][string]$Url,
        [string]$Method = "GET",
        [hashtable]$Headers,
        [object]$Body = $null
    )

    $safeName = ($Name -replace '[^a-zA-Z0-9._-]', '_')
    $targetFile = Join-Path $OutputDir ("{0}.json" -f $safeName)

    $payload = [ordered]@{
        capturedAtUtc = [DateTime]::UtcNow.ToString("o")
        name = $Name
        method = $Method
        url = $Url
        success = $false
        statusCode = $null
        reason = $null
        headers = @{}
        responseBody = $null
        error = $null
    }

    try {
        $params = @{
            Uri = $Url
            Method = $Method
            TimeoutSec = 20
            ErrorAction = "Stop"
            UseBasicParsing = $true
        }

        if ($Headers) {
            $params.Headers = $Headers
        }

        if ($Body -ne $null) {
            $params.ContentType = "application/json"
            $params.Body = ($Body | ConvertTo-Json -Depth 10)
        }

        $response = Invoke-WebRequest @params
        $payload.success = $true
        $payload.statusCode = [int]$response.StatusCode
        $payload.reason = $response.StatusDescription

        foreach ($headerKey in $response.Headers.Keys) {
            $payload.headers[$headerKey] = @($response.Headers[$headerKey])
        }

        try {
            $parsed = $response.Content | ConvertFrom-Json -ErrorAction Stop
            $payload.responseBody = $parsed
        }
        catch {
            $payload.responseBody = $response.Content
        }
    }
    catch {
        $ex = $_.Exception
        $payload.error = $ex.Message

        if ($ex.Response) {
            try {
                $payload.statusCode = [int]$ex.Response.StatusCode.value__
                $payload.reason = [string]$ex.Response.StatusCode
            }
            catch { }

            try {
                $stream = $ex.Response.GetResponseStream()
                if ($stream) {
                    $reader = New-Object System.IO.StreamReader($stream)
                    $raw = $reader.ReadToEnd()
                    if (-not [string]::IsNullOrWhiteSpace($raw)) {
                        try {
                            $payload.responseBody = ($raw | ConvertFrom-Json -ErrorAction Stop)
                        }
                        catch {
                            $payload.responseBody = $raw
                        }
                    }
                }
            }
            catch { }
        }
    }

    $snapshot = [pscustomobject]$payload
    $snapshot | ConvertTo-Json -Depth 20 | Set-Content -Path $targetFile -Encoding UTF8
    return $snapshot
}

Ensure-Dir -Path $OutputDir

$apiToken = New-LegacyToken -Secret "F8FD63BDD36C4AF98CA80A741A7A1C71" -Subject "admin" -ExpiryMinutes 120
$restToken = New-LegacyToken -Secret "469F0FC801BA49E0A2F0B5378AA3EA46" -Subject "admin" -ExpiryMinutes 120

$apiAuthHeaders = @{ Authorization = "Bearer $apiToken" }
$restAuthHeaders = @{ Authorization = "Bearer $restToken" }
$apiCredentialHeaders = @{ username = "admin"; password = "password123" }
$restCredentialHeaders = @{ username = "admin"; password = "password123" }

$results = @()

# API Token/Auth endpoints
$results += Save-Snapshot -Name "api_token_header" -Url "$ApiBaseUrl/api/Token" -Headers $apiCredentialHeaders
$results += Save-Snapshot -Name "api_token_path" -Url "$ApiBaseUrl/api/Token/admin/password123"

# API User info and orders
$results += Save-Snapshot -Name "api_userinfo_current" -Url "$ApiBaseUrl/api/UserInfo" -Headers $apiAuthHeaders
$results += Save-Snapshot -Name "api_userinfo_by_username" -Url "$ApiBaseUrl/api/UserInfo/admin" -Headers $apiAuthHeaders
$results += Save-Snapshot -Name "api_joborders_list" -Url "$ApiBaseUrl/api/JobOrders" -Headers $apiAuthHeaders
$results += Save-Snapshot -Name "api_prints_pending" -Url "$ApiBaseUrl/api/Prints/Pending" -Headers $apiAuthHeaders
$results += Save-Snapshot -Name "api_prints_scheduled" -Url "$ApiBaseUrl/api/Prints/Scheduled" -Headers $apiAuthHeaders
$results += Save-Snapshot -Name "api_prints_scheduled_machine_1" -Url "$ApiBaseUrl/api/Prints/Scheduled/1" -Headers $apiAuthHeaders
$results += Save-Snapshot -Name "api_prints_completed" -Url "$ApiBaseUrl/api/Prints/Completed" -Headers $apiAuthHeaders
$results += Save-Snapshot -Name "api_prints_completed_machine_1" -Url "$ApiBaseUrl/api/Prints/Completed/1" -Headers $apiAuthHeaders

# REST Token/Auth endpoints
$results += Save-Snapshot -Name "rest_token_header" -Url "$RestBaseUrl/api/Token" -Headers $restCredentialHeaders
$results += Save-Snapshot -Name "rest_token_path" -Url "$RestBaseUrl/api/Token/admin/password123"
$results += Save-Snapshot -Name "rest_user_current" -Url "$RestBaseUrl/api/User" -Headers $restAuthHeaders

# REST domain endpoints with safe GET probes
$results += Save-Snapshot -Name "rest_job_range" -Url "$RestBaseUrl/api/Job/2026-03-27/10" -Headers $restAuthHeaders
$results += Save-Snapshot -Name "rest_job_by_month_all" -Url "$RestBaseUrl/api/Job/ByMonth/0/2026-03-27" -Headers $restAuthHeaders
$results += Save-Snapshot -Name "rest_quotation_range" -Url "$RestBaseUrl/api/Qt/2026-03-27/10" -Headers $restAuthHeaders
$results += Save-Snapshot -Name "rest_quotation_keyword" -Url "$RestBaseUrl/api/Qt/Keyword/ABC" -Headers $restAuthHeaders

$meta = [ordered]@{
    capturedAtUtc = [DateTime]::UtcNow.ToString("o")
    apiBaseUrl = $ApiBaseUrl
    restBaseUrl = $RestBaseUrl
    attempted = $results.Count
    succeeded = @($results | Where-Object { $_.success }).Count
    failed = @($results | Where-Object { -not $_.success }).Count
    outputDir = $OutputDir
}
$meta | ConvertTo-Json -Depth 6 | Set-Content -Path (Join-Path $OutputDir "snapshot-metadata.json") -Encoding UTF8

$summary = $results | Select-Object name, success, statusCode, reason
$summary | ConvertTo-Json -Depth 4 | Set-Content -Path (Join-Path $OutputDir "snapshot-summary.json") -Encoding UTF8

Write-Host ("Snapshots attempted: {0}, succeeded: {1}, failed: {2}" -f $meta.attempted, $meta.succeeded, $meta.failed)
