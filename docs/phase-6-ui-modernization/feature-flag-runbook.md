# UI Feature Flag Runbook

## Scope

This runbook covers the configuration-backed UI slice flags hosted by `JB2026.WebApp` during Phase 6 coexistence.

## Flag Store

- Source: `UiModernization` section in `JB2026.WebApp/appsettings*.json` or environment-variable overrides.
- Cache policy: each slice flag is cached for up to 60 seconds in memory.
- Failure mode: if a slice is disabled and no `LegacyBaseUrl` is configured, the web app serves the local legacy placeholder page instead of the SPA.

## Supported Slices

- `dashboard`
- `jobs`
- `quotations`
- `forms`
- `editor`
- `scheduler`

## Toggle Procedure

1. Update the target slice value under `UiModernization:Slices:<slice>:Enabled`.
2. If the environment should redirect disabled traffic to a live JB2015 host, set `UiModernization:LegacyBaseUrl` to the external legacy site root.
3. Wait 60 seconds for the in-memory cache window to expire.
4. Verify the route manually.

## Verification Steps

1. Open `GET /ui/feature-flags` and confirm the slice state matches the intended configuration.
2. Request the slice route directly, for example `/jobs`.
3. When enabled, confirm the response serves the SPA shell from `/app/index.html`.
4. When disabled, confirm the request redirects to the configured legacy base URL or falls back to the local legacy placeholder page.

## Recommended Environment Overrides

- `UiModernization__LegacyBaseUrl=https://legacy.example.internal`
- `UiModernization__Slices__jobs__Enabled=false`
- `UiModernization__Slices__editor__Enabled=true`

## Rollback

1. Set the slice `Enabled` flag back to `false`.
2. Wait one cache TTL window.
3. Re-verify the route resolves through the legacy path.