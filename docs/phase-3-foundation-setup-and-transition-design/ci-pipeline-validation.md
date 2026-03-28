# CI Pipeline Validation

## Pipeline Definition
- File: .github/workflows/ci.yml
- Trigger: pull_request (all branches) and push to main.

## Blocking Gates
- Build, Test, and Lint
- Parity Tests (Phase 4 backend/API migration)
- Security Scan Gate
- License Scan Gate

## Local End-to-End Validation Steps
1. dotnet build JB2026.sln --configuration Release
2. dotnet test JB2026.sln --configuration Release
3. dotnet test JB2026.Api.ParityTests/JB2026.Api.ParityTests.csproj --configuration Release
4. dotnet format JB2026.sln --verify-no-changes
5. ./scripts/security-scan.ps1
6. ./scripts/license-scan.ps1

## Expected Outcome
Any failing stage blocks merge because dependent jobs require successful completion of prior gates.
