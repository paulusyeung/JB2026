## 1. Backend API

- [x] 1.1 Create `JB2026.Api/Controllers/UserPreferencesController.cs` with GET and PUT endpoints
- [x] 1.2 Implement `GetCurrentUserId()` helper to extract user ID from JWT
- [x] 1.3 Add EF Core DbSet<UserPreference> if not already configured
- [x] 1.4 Verify auth middleware is applied to the controller
- [x] 1.5 Test endpoints manually with Postman/curl

## 2. Frontend Service and Constants

- [x] 2.1 Create `src/services/userPreferences.ts` with `getUserPreference()` and `saveUserPreference()`
- [x] 2.2 Create `src/composables/viewPreferenceKeys.ts` with static GUIDs and ObjectType constants
- [x] 2.3 Add TypeScript types for preference request/response

## 3. Frontend Composable Update

- [x] 3.1 Update `src/composables/useColumnPersistence.ts` to import the new service and constants
- [x] 3.2 Implement server load on mount with localStorage fallback
- [x] 3.3 Implement debounced save (500ms) with localStorage sync
- [x] 3.4 Ensure backward compatibility — existing localStorage data is preserved

## 4. Integration and Testing

- [x] 4.1 Verify StockView loads settings from server on mount
- [x] 4.2 Verify StockView saves settings to server on change
- [x] 4.3 Verify localStorage fallback works when API is unavailable
- [x] 4.4 Verify debouncing prevents excessive API calls
- [x] 4.5 Verify existing localStorage data is not lost during migration
- [ ] 4.6 Run `pnpm run typecheck` to ensure no type errors

## 5. Documentation and Cleanup

- [x] 5.1 Add inline comments to `viewPreferenceKeys.ts` explaining the GUID assignment
- [x] 5.2 Document the migration path in the composable's JSDoc
- [x] 5.3 Remove any temporary debug logging
