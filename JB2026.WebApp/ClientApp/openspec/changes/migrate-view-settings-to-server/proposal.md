## Why

The application currently persists view settings (visible columns, sorting, checkbox mode, and view mode) to `localStorage` via the `useViewSettings` composable. While functional for single-device use, this approach has several limitations:

1. **No cross-device sync** — Users cannot access their customized view settings from different devices.
2. **No server-side backup** — Browser data clearing or device loss results in permanent loss of user preferences.
3. **No multi-user support** — Shared devices cannot maintain separate view configurations.

The `UserPreference` table in `dbo` already exists in the database schema and is designed to store per-user, per-object metadata. Migrating view settings to this table leverages existing infrastructure and aligns with the application's long-term architecture.

## What Changes

- **New API endpoint**: Add `GET/PUT /api/v2/user-preferences/{objectType}/{objectId}` to the backend for reading and persisting user preferences.
- **New client service**: Create `src/services/userPreferences.ts` with `getUserPreference()` and `saveUserPreference()` functions.
- **New composable helper**: Create `src/composables/viewPreferenceKeys.ts` with static GUIDs for each view's ObjectId and integer constants for ObjectType.
- **Updated composable**: Modify `src/composables/useColumnPersistence.ts` to load from the server API on mount and debounce-save changes, while keeping `localStorage` as a fallback.
- **Migration path**: Existing `localStorage` data is preserved and used as a fallback; server records are created on first write.

## Capabilities

### New Capabilities
- `server-side-view-preferences`: Persist view settings (columns, sorting, checkbox mode, view mode) to the server's UserPreference table for cross-device sync.
- `user-preference-api`: RESTful API for reading and saving per-user, per-object preferences with JSON metadata.

### Modified Capabilities
- None.

## Impact

- **Backend**: New `UserPreferencesController` in `JB2026.Api/Controllers/`.
- **Frontend**: New service file `src/services/userPreferences.ts`, new constants file `src/composables/viewPreferenceKeys.ts`, updated `src/composables/useColumnPersistence.ts`.
- **Database**: No schema changes — uses existing `UserPreference` table (`UserId`, `ObjectId`, `ObjectType`, `MetadataXml`).
- **Existing views**: `StockView.vue` and any future views using `useViewSettings` automatically benefit from server sync.
