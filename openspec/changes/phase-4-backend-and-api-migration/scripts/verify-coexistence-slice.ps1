param(
    [string]$BaseUrl = "http://localhost:8000",
    [string]$Username = "admin",
    [string]$Password = "password123",
    [string]$OutputPath = "C:\Projects\JB2026\openspec\changes\phase-4-backend-and-api-migration\preprod-coexistence-verification.json",
    [int]$TimeoutSec = 30
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Invoke-Endpoint {
    param(
        [Parameter(Mandatory = $true)][string]$Method,
        [Parameter(Mandatory = $true)][string]$Url,
        [hashtable]$Headers,
        [object]$Body
    )

    $result = [ordered]@{
        method = $Method
        url = $Url
        ok = $false
        statusCode = $null
        reason = $null
        body = $null
        error = $null
    }

    try {
        $params = @{
            Uri = $Url
            Method = $Method
            TimeoutSec = $TimeoutSec
            ErrorAction = "Stop"
            UseBasicParsing = $true
        }

        if ($Headers) {
            $params.Headers = $Headers
        }

        if ($Body) {
            $params.ContentType = "application/json"
            $params.Body = ($Body | ConvertTo-Json -Depth 10)
        }

        $response = Invoke-WebRequest @params
        $result.ok = $true
        $result.statusCode = [int]$response.StatusCode
        $result.reason = [string]$response.StatusDescription

        if (-not [string]::IsNullOrWhiteSpace($response.Content)) {
            try {
                $result.body = $response.Content | ConvertFrom-Json -ErrorAction Stop
            }
            catch {
                $result.body = $response.Content
            }
        }
    }
    catch {
        $result.error = $_.Exception.Message
        if ($_.Exception.Response) {
            $result.statusCode = [int]$_.Exception.Response.StatusCode.value__
            $result.reason = [string]$_.Exception.Response.StatusCode
        }
    }

    return [pscustomobject]$result
}

function Get-Token {
    param([Parameter(Mandatory = $true)][string]$Version)

    $authUrl = "$BaseUrl/api/$Version/auth/token"
    $authBody = @{ username = $Username; password = $Password }
    $tokenResponse = Invoke-Endpoint -Method "POST" -Url $authUrl -Body $authBody

    if (-not $tokenResponse.ok) {
        return [pscustomobject]@{
            version = $Version
            ok = $false
            accessToken = $null
            statusCode = $tokenResponse.statusCode
            reason = $tokenResponse.reason
            error = $tokenResponse.error
        }
    }

    $token = $null
    if ($tokenResponse.body -and $tokenResponse.body.accessToken) {
        $token = [string]$tokenResponse.body.accessToken
    }

    return [pscustomobject]@{
        version = $Version
        ok = -not [string]::IsNullOrWhiteSpace($token)
        accessToken = $token
        statusCode = $tokenResponse.statusCode
        reason = $tokenResponse.reason
        error = if ($token) { $null } else { "Token missing in response body." }
    }
}

$tokenV1 = Get-Token -Version "v1"
$tokenV2 = Get-Token -Version "v2"
$tokenReady = ($tokenV1.ok -and $tokenV2.ok)

$checks = @()

$endpoints = @(
    @{ name = "quotations_search"; v1Path = "/api/v1/quotations/search/ABC"; v2Path = "/api/v2/quotations/search/ABC" },
    @{ name = "quotations_range"; v1Path = "/api/v1/quotations?startOn=2026-03-27&days=10"; v2Path = "/api/v2/quotations?startOn=2026-03-27&days=10" },
    @{ name = "jobs_range"; v1Path = "/api/v1/jobs/range?startOn=2026-03-27&days=10"; v2Path = "/api/v2/jobs/range?startOn=2026-03-27&days=10" },
    @{ name = "job_orders_list"; v1Path = "/api/v1/job-orders"; v2Path = "/api/v2/job-orders" }
)

foreach ($endpoint in $endpoints) {
    $headersV1 = if ($tokenV1.ok) { @{ Authorization = "Bearer $($tokenV1.accessToken)" } } else { @{} }
    $headersV2 = if ($tokenV2.ok) { @{ Authorization = "Bearer $($tokenV2.accessToken)" } } else { @{} }

    $v1 = Invoke-Endpoint -Method "GET" -Url ("$BaseUrl" + $endpoint.v1Path) -Headers $headersV1
    $v2 = Invoke-Endpoint -Method "GET" -Url ("$BaseUrl" + $endpoint.v2Path) -Headers $headersV2

    $countV1 = $null
    $countV2 = $null

    if ($v1.ok -and $v1.body -is [System.Collections.IEnumerable] -and -not ($v1.body -is [string])) {
        try { $countV1 = @($v1.body).Count } catch { }
    }
    if ($v2.ok -and $v2.body -is [System.Collections.IEnumerable] -and -not ($v2.body -is [string])) {
        try { $countV2 = @($v2.body).Count } catch { }
    }

    $statusParity = ($null -ne $v1.statusCode -and $null -ne $v2.statusCode -and $v1.statusCode -eq $v2.statusCode)
    $countParity = ($null -ne $countV1 -and $null -ne $countV2 -and $countV1 -eq $countV2)

    $checks += [pscustomobject]@{
        name = $endpoint.name
        v1Status = $v1.statusCode
        v2Status = $v2.statusCode
        statusParity = $statusParity
        v1Count = $countV1
        v2Count = $countV2
        countParity = $countParity
        pass = ($tokenReady -and $v1.ok -and $v2.ok -and $statusParity -and $countParity)
    }
}

$report = [ordered]@{
    capturedAtUtc = [DateTime]::UtcNow.ToString("o")
    baseUrl = $BaseUrl
    tokens = @($tokenV1, $tokenV2)
    checks = $checks
    summary = [ordered]@{
        attempted = $checks.Count
        passed = @($checks | Where-Object { $_.pass }).Count
        failed = @($checks | Where-Object { -not $_.pass }).Count
    }
}

$report | ConvertTo-Json -Depth 20 | Set-Content -Path $OutputPath -Encoding UTF8

Write-Host ("Coexistence checks attempted: {0}, passed: {1}, failed: {2}" -f $report.summary.attempted, $report.summary.passed, $report.summary.failed)

if ($report.summary.failed -gt 0) {
    exit 1
}
