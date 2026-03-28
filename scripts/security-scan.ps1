$ErrorActionPreference = "Stop"

Write-Host "Running vulnerability scan against JB2026.sln..."
$report = dotnet list JB2026.sln package --vulnerable --include-transitive --format json | Out-String

if ([string]::IsNullOrWhiteSpace($report)) {
    throw "Security scan did not produce output."
}

if ($report -match '"severity"\s*:') {
    Write-Error "Vulnerable packages were detected. Review 'dotnet list package --vulnerable --include-transitive'."
    exit 1
}

Write-Host "No vulnerable packages detected."
