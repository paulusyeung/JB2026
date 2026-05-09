# Refresh Token Authentication - Testing Guide

This document provides scenarios for testing the refresh token authentication feature.

## Test Environment Setup

Before running tests, ensure:
1. The API is running (e.g., `dotnet run` in JB2026.Api)
2. The Frontend app is running (e.g., `npm run dev` in ClientApp)
3. Browser Dev Tools are open to inspect localStorage and network requests

## Test Scenarios

### 8.1 Login with keepMeSignedIn: true verifies refresh token is issued and stored

**Steps:**
1. Navigate to the login page
2. Check the "Keep Me Signed In" checkbox
3. Enter valid credentials and submit
4. Inspect browser localStorage to verify:
   - `jb2026.accessToken` is present
   - `jb2026.refreshToken` is present

**Expected Result:**
- Both access token and refresh token are stored in localStorage
- User is redirected to the dashboard

---

### 8.2 Login with keepMeSignedIn: false verifies no refresh token is issued

**Steps:**
1. Navigate to the login page
2. Ensure "Keep Me Signed In" checkbox is unchecked
3. Enter valid credentials and submit
4. Inspect browser localStorage to verify:
   - `jb2026.accessToken` is present
   - `jb2026.refreshToken` is NOT present (or is empty)

**Expected Result:**
- Only access token is stored in localStorage
- No refresh token is stored
- User is redirected to the dashboard

---

### 8.3 Auto-refresh on 401: request succeeds after token renewal

**Steps:**
1. Login with "Keep Me Signed In" checked
2. Open browser Dev Tools → Network tab
3. Wait for the access token to expire (configured as 60 minutes, can be shortened for testing)
4. Make an API request (navigate to a page that triggers an API call)
5. Observe in Network tab:
   - Initial request receives 401 response
   - Auto refresh request to `/api/v2/auth/refresh` is made
   - Original request is retried with new token and succeeds (200)

**Expected Result:**
- User doesn't see an error or login page redirect
- API request eventually succeeds
- New tokens are stored in localStorage

---

### 8.4 Concurrent requests during refresh: all succeed after single refresh

**Steps:**
1. Login with "Keep Me Signed In" checked
2. When access token is about to expire, trigger multiple API requests simultaneously
   - Can use browser console: `fetch('/api/v2/dashboard')`  multiple times
3. Observe in Network tab:
   - Only ONE refresh request is made (not multiple)
   - All pending requests are queued and retried together
   - All requests eventually succeed

**Expected Result:**
- Only one refresh request is sent to the server
- Multiple original requests wait for the refresh to complete
- All requests then succeed with the new token

---

### 8.5 Logout: refresh token is revoked and cleared from storage

**Steps:**
1. Login with "Keep Me Signed In" checked
2. Verify `jb2026.refreshToken` is in localStorage
3. Click logout button
4. Inspect browser localStorage:
   - `jb2026.accessToken` is removed
   - `jb2026.refreshToken` is removed
5. Check API logs or Network tab:
   - Should see a POST request to `/api/v2/auth/revoke` before logout

**Expected Result:**
- Both tokens are cleared from localStorage
- Refresh token is revoked on the server
- User is redirected to login page

---

### 8.6 Expired refresh token: user is redirected to login

**Steps:**
1. Login with "Keep Me Signed In" checked
2. Wait for both access token and refresh token to expire
   - Access token expires after 60 minutes
   - Refresh token expires after 30 days (default)
   - Can modify expiryDays in code for testing
3. When both are expired, attempt an API request
4. Observe in Network tab:
   - Initial request gets 401
   - Refresh request also fails with 401 (refresh token expired)
   - User is redirected to login page

**Expected Result:**
- Session is cleared
- User is redirected to login
- Must log in again to continue

---

### 8.7 Token theft detection: reused refresh token revokes all user tokens

**Steps:**
1. Login with "Keep Me Signed In" checked
2. Copy the refresh token from localStorage (`jb2026.refreshToken`)
3. Make a request to `/api/v2/auth/refresh` with the token (e.g., using Postman)
4. This returns a new refresh token
5. Try to use the original (old) refresh token again to make another `/api/v2/auth/refresh` request
6. Observe:
   - Second refresh request fails with 401
   - Error message indicates "invalid_refresh_token"
   - All refresh tokens for that user should be revoked

**Expected Result:**
- Reusing an old refresh token is detected as potential theft
- All user's refresh tokens are revoked
- User must log in again

---

### 8.8 Revoke idempotency: unknown/already-invalid refresh token returns HTTP 204

**Steps:**
1. Call the `/api/v2/auth/revoke` endpoint with:
   - A non-existent token
   - An already-revoked token
2. Observe HTTP response:
   - First revoke of a valid token: 204 No Content
   - Subsequent revokes of the same token: 204 No Content (idempotent)
   - Revoke of invalid/unknown token: 204 No Content

**Expected Result:**
- All revoke requests return HTTP 204
- No error is returned even for unknown tokens (idempotent behavior)

---

## Automated Test Script (Postman/cURL)

### Test Token Refresh Flow

```bash
# 1. Login with keepMeSignedIn=true
curl -X POST http://localhost:5000/api/v2/auth/token \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"password123","keepMeSignedIn":true}'

# Save the tokens from the response
# ACCESS_TOKEN=<accessToken from response>
# REFRESH_TOKEN=<refreshToken from response>

# 2. Test refresh endpoint
curl -X POST http://localhost:5000/api/v2/auth/refresh \
  -H "Content-Type: application/json" \
  -d "{\"refreshToken\":\"$REFRESH_TOKEN\"}"

# 3. Test revoke endpoint
curl -X POST http://localhost:5000/api/v2/auth/revoke \
  -H "Content-Type: application/json" \
  -d "{\"refreshToken\":\"$REFRESH_TOKEN\"}"
```

---

## Summary

The refresh token authentication feature is now complete and ready for integration testing. All functional requirements have been implemented:

- ✅ Refresh token issuance on login when "Keep Me Signed In" is checked
- ✅ Token exchange/refresh endpoint with rotation
- ✅ Token revocation endpoint
- ✅ Automatic token refresh on 401 (frontend interceptor)
- ✅ Request queuing during refresh
- ✅ Token theft detection
- ✅ Session store integration
- ✅ Login UI integration

Use these test scenarios to validate the feature before deployment.
