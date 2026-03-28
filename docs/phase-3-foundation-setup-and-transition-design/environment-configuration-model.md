# Environment Configuration Model

## Purpose
Define a single configuration model for Development, Test, PreProduction, and Production without code changes.

## Configuration Sources and Precedence
1. appsettings.json
2. appsettings.{Environment}.json
3. User Secrets (Development only)
4. Environment variables

Higher-precedence sources override lower-precedence values.

## Approved Secret Injection Mechanism
- Primary mechanism: environment variables injected by deployment platform.
- Development mechanism: user secrets plus environment variables.
- Disallowed mechanism: hardcoded credentials or plain-text secrets in repository files.

## Required Variables
- ASPNETCORE_ENVIRONMENT
- ConnectionStrings__Primary
- JB2026__Environment__DeploymentRing
- JB2026__Environment__SecretProvider
- JB2026__Observability__OtlpEndpoint

## Environment Profiles
| Environment | Deployment Ring | Secret Provider | Notes |
|---|---|---|---|
| Development | Development | UserSecretsAndEnvironmentVariables | Local developer profile |
| Test | Test | EnvironmentVariables | CI and shared test environment |
| PreProduction | PreProduction | EnvironmentVariables | Staging and validation |
| Production | Production | EnvironmentVariables | Production runtime |

## Validation Checklist
- No secrets committed to source control.
- Environment-specific values set only through approved providers.
- Configuration loads successfully when only environment variables are present.
