## Context

The application currently uses `localStorage` to persist view settings (visible columns, sorting, checkbox mode, and view mode) via the `useViewSettings` composable in `src/composables/useColumnPersistence.ts`. Each view (e.g., 'stock', 'orders') stores its settings under the key `view-settings-{viewId}`.

The backend already has a `UserPreference` entity in `JB2026.EfCore.Models` with the following schema:
- `PreferenceId` (Guid, PK)
- `UserId` (Guid) — the authenticated user
- `ObjectId` (Guid) — identifies the target object (e.g., a specific view)
- `ObjectType` (int) — categorizes the preference type
- `MetadataXml` (string?) — stores JSON metadata

No API endpoint currently exposes this table to the frontend.

## Goals / Non-Goals

**Goals:**
- Provide a server-backed API for reading and saving view settings per user.
- Maintain backward compatibility with existing `localStorage` data.
- Use static GUIDs as ObjectId values for each view, avoiding string-based identifiers.
- Use integer constants for ObjectType on the client; C# enum on the server for type safety.
- Implement debounced saves to minimize API calls during rapid UI interactions.
- Keep `localStorage` as a fallback mechanism.

**Non-Goals:**
- Migrating other user preferences (e.g., theme, date format) — those are handled by separate stores.
- Real-time sync across devices — changes are persisted on debounce, not pushed in real-time.
- Bulk operations or preference templates.
- Database schema changes to the `UserPreference` table.

## Decisions

### 1. Static GUID as ObjectId per view
- **Decision**: Each view gets a fixed GUID stored in a constants file (`viewPreferenceKeys.ts`).
- **Rationale**: Matches the EF model's `Guid` type, avoids string encoding issues, and provides stability across migrations.
- **Alternatives considered**:
  - Using the viewId string directly — rejected due to type mismatch with EF model and potential encoding issues.
  - Generating GUIDs dynamically — rejected because it would create duplicate records per view.

### 2. Integer constants for ObjectType on client, enum on server
- **Decision**: Client uses `const OBJECT_TYPE_VIEW_SETTINGS = 1`; server uses a C# enum.
- **Rationale**: Avoids sync risk between client and server enum values. The server enum provides compile-time safety in C# code; the client uses simple integers with no overhead.
- **Alternatives considered**:
  - TypeScript enum synced with C# enum — rejected due to maintenance burden and silent corruption risk on mismatch.

### 3. Debounced save (500ms) with localStorage fallback
- **Decision**: Changes trigger a debounced API save; localStorage is updated simultaneously as a fallback.
- **Rationale**: Prevents excessive API calls during rapid UI interactions (e.g., toggling multiple columns). localStorage ensures settings persist even if the API is unavailable.
- **Alternatives considered**:
  - Save on every change — rejected due to API call overhead.
  - Save only on unmount — rejected because users might lose settings if the tab is closed unexpectedly.

### 4. Initialization: localStorage first, then server overlay
- **Decision**: On mount, load from localStorage immediately (instant feedback), then overlay server data if available.
- **Rationale**: Provides instant UI responsiveness while still syncing with the server. If the server has newer data, it overrides localStorage.
- **Alternatives considered**:
  - Server-only — rejected because it adds latency to initial render.
  - localStorage-only — rejected because it defeats the purpose of server sync.

## Risks / Trade-offs

| Risk | Mitigation |
|------|-----------|
| API unavailable during save | localStorage fallback ensures no data loss |
| Stale data on concurrent devices | Last-write-wins via server; users see their most recent change |
| Migration of existing localStorage data | Existing data is preserved and used as fallback; server record created on first write |
| Large metadata payloads | View settings are small (columns array, sort key, etc.); no size concerns |

## Migration Plan

1. **Backend**: Add `UserPreferencesController` with GET/PUT endpoints.
2. **Frontend**: Create `userPreferences.ts` service and `viewPreferenceKeys.ts` constants.
3. **Frontend**: Update `useColumnPersistence.ts` to use the new service.
4. **Verification**: Test that existing localStorage data is preserved, server sync works, and debouncing functions correctly.
5. **Rollback**: If issues arise, revert the composable to localStorage-only mode; the API endpoint can remain unused.

## Open Questions

- Should we add a `GET /api/v2/user-preferences/{objectType}` endpoint to fetch all preferences for a type in one call? (Out of scope for this change.)
- Should we add a migration script to backfill existing localStorage data into the server? (Deferred — users will migrate organically on first interaction.)
