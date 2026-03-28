$ErrorActionPreference = "Stop"

$infrastructureProject = "JB2026.Infrastructure/JB2026.Infrastructure.csproj"
$licenseMatrix = "docs/phase-0-governance/dependency-license-matrix.md"

if (-not (Test-Path $infrastructureProject)) {
    throw "Missing project file: $infrastructureProject"
}

if (-not (Test-Path $licenseMatrix)) {
    throw "Missing dependency matrix: $licenseMatrix"
}

[xml]$projectXml = Get-Content -Path $infrastructureProject
$packageIds = $projectXml.Project.ItemGroup.PackageReference | ForEach-Object { $_.Include } | Sort-Object -Unique

$approvedLicenses = @{
    "Microsoft.Extensions.Configuration.UserSecrets" = "MIT"
    "OpenTelemetry.Exporter.Console" = "Apache-2.0"
    "OpenTelemetry.Exporter.OpenTelemetryProtocol" = "Apache-2.0"
    "OpenTelemetry.Extensions.Hosting" = "Apache-2.0"
    "OpenTelemetry.Instrumentation.AspNetCore" = "Apache-2.0"
    "OpenTelemetry.Instrumentation.Http" = "Apache-2.0"
    "Serilog.AspNetCore" = "Apache-2.0"
    "Serilog.Settings.Configuration" = "MIT"
}

$matrixContent = Get-Content -Path $licenseMatrix -Raw
$errors = @()

foreach ($packageId in $packageIds) {
    if (-not $approvedLicenses.ContainsKey($packageId)) {
        $errors += "Package '$packageId' is not listed in the approved license map."
        continue
    }

    if ($matrixContent -notmatch [Regex]::Escape($packageId)) {
        $errors += "Package '$packageId' is missing from dependency license matrix."
    }
}

$disallowedPatterns = @("DevExpress", "Thinktecture", "EnterpriseLibrary", "Katana", "OWIN")
$packageReport = dotnet list JB2026.sln package --include-transitive --format json | Out-String
$packageIdMatches = [Regex]::Matches($packageReport, '"id"\s*:\s*"([^"]+)"')
$allPackageIds = @($packageIdMatches | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique)

foreach ($pattern in $disallowedPatterns) {
    if ($allPackageIds | Where-Object { $_ -match $pattern }) {
        $errors += "Disallowed dependency pattern detected in package graph: $pattern"
    }
}

if ($errors.Count -gt 0) {
    Write-Error ($errors -join [Environment]::NewLine)
    exit 1
}

Write-Host "License scan passed for infrastructure dependencies and disallowed package checks."
