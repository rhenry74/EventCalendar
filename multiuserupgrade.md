# Multi-User Upgrade Plan

## Overview
This document outlines the implementation plan for adding multi-user functionality to the EventCalendar application using Google OAuth authentication and JSON file-based storage.

---

## Architecture

### Backend (.NET API)
1. **Google Authentication** using `Microsoft.Identity.Web` library
2. **JSON File Storage** - Separate files per collection:
   - `users.json` - User accounts with Google subject ID as unique identifier
   - `events.json` - Events with owner reference (user's Google subject ID)
3. **File Locking**: Simple timestamp-based versioning (check/modify/save pattern) to prevent race conditions
4. **API Endpoints**:
   - `/api/auth/login` - Google OAuth callback handler
   - `/api/auth/me` - Get current authenticated user info
   - `GET /api/events?userId={id}` - List events filtered by user ID
   - `POST /api/events` - Create event (authenticated only)
   - `PUT/DELETE /api/events/{id}` - Update/delete user's own events

### Frontend (React + Vite)
1. **Authentication State**: Static singleton service (`AuthService`) that reads JWT claims from the cookie/header
2. **Login Page**: Redirect to Google OAuth, handle callback with token
3. **Protected Routes**: Show calendar only when authenticated
4. **Event Management**: CRUD operations for the logged-in user's events

---

## Database Schema (JSON Files)

### users.json
```json
[
  {
    "subject": "1234567890-abcdef...@apps.googleusercontent.com",
    "displayName": "John Doe",
    "email": "john.doe@example.com",
    "picture": "https://lh3.googleusercontent.com/photo.jpg"
  }
]
```

### events.json
```json
[
  {
    "id": "event-123",
    "title": "Team Meeting",
    "description": "...",
    "startDate": "2026-08-15T10:00:00Z",
    "endDate": "2026-08-15T11:00:00Z",
    "ownerId": "1234567890-abcdef...@apps.googleusercontent.com"
  }
]
```

---

## Implementation Checklist

### Phase 1: Backend Setup
- [ ] Install `Microsoft.Identity.Web` package for Google OAuth
- [ ] Configure Google OAuth in Program.cs (add authentication builder)
- [ ] Create Data folder structure (`Data/`)
- [ ] Implement `IFileLock` interface and implementation
- [ ] Implement `JsonStorage<T>` generic storage wrapper with file locking

### Phase 2: Authentication Endpoints
- [ ] Add `[Authorize]` attribute to protected endpoints
- [ ] Create `/api/auth/login` endpoint for OAuth callback
- [ ] Create `/api/auth/me` endpoint to get current user info
- [ ] Create `/api/users` endpoint (list all users)

### Phase 3: Event Endpoints with User Filtering
- [ ] Update `GET /api/events` to accept and filter by `userId` query parameter
- [ ] Add `POST /api/events` for creating events (requires authentication)
- [ ] Add `PUT /api/events/{id}` for updating events (owner-only)
- [ ] Add `DELETE /api/events/{id}` for deleting events (owner-only)

### Phase 4: Frontend Authentication
- [ ] Create `AuthService.ts` singleton service
- [ ] Implement login flow with Google OAuth redirect
- [ ] Store JWT token in cookie/header after successful authentication
- [ ] Create logout functionality
- [ ] Add auth state to React context or use static service

### Phase 5: Frontend UI Components
- [ ] Create `LoginPage.tsx` with "Login with Google" button
- [ ] Update `App.tsx` to conditionally render based on auth state
- [ ] Wrap calendar in `<ProtectedRoute>` component
- [ ] Update event forms to show current user's name/email

### Phase 6: Testing & Refinement
- [ ] Test login/logout flow
- [ ] Verify users can only see their own events
- [ ] Test concurrent file access (file locking)
- [ ] Add error handling for auth failures
- [ ] Update README with setup instructions

---

## File Structure Changes

```
EventCalendar.API/
├── Data/                    # NEW: Storage layer
│   ├── IFileLock.cs         # NEW: File locking interface
│   └── JsonStorage.cs       # NEW: Generic JSON storage wrapper
├── Controllers/
│   ├── AuthController.cs    # NEW: Authentication endpoints
│   └── EventController.cs   # UPDATED: User-filtered events
├── Program.cs               # UPDATED: Add OAuth configuration
├── users.json               # NEW: User accounts storage
└── events.json              # UPDATED: Add ownerId field

src/
├── services/
│   └── AuthService.ts       # NEW: Authentication service singleton
├── LoginPage.tsx            # NEW: Login page component
└── App.tsx                  # UPDATED: Protected routes
```

---

## Google OAuth Setup (Prerequisites)

1. Go to Google Cloud Console
2. Create OAuth 2.0 Client IDs
3. Configure authorized redirect URIs:
   - `https://localhost:7051/api/auth/login` (backend callback)
4. Copy Client ID and Client Secret for configuration

---

## Notes

- **File Locking**: Uses timestamp-based versioning to prevent race conditions when multiple users access files simultaneously
- **Security**: JWT tokens stored in secure HTTP-only cookies
- **Scalability**: JSON file storage is suitable for small-scale/local development; can be migrated to Entity Framework/SQL Server later
- **User Identity**: Google Subject ID used as unique user identifier across the application

---

## Current Problems & Solutions (Program.cs)

### Problem 1: Namespace Declaration Order
**Issue**: File-scoped namespace `namespace EventCalendar.API;` must come BEFORE all top-level statements, but current code has top-level statements before it.

**Solution**: Move all type definitions (`User`, `Event`) to BEFORE the namespace declaration, or remove the namespace entirely and use regular class declarations after `app.Run()`.

---

### Problem 2: Type Definition Scope
**Issue**: The `Event` and `User` classes are defined after `app.Run()`, making them inaccessible to earlier code that references them (e.g., in default events initialization).

**Solution**: Move all type definitions (`User`, `Event`) to the END of the file, AFTER all top-level statements. This ensures they're accessible throughout the entire Program.cs file.

---

### Problem 3: CookiePolicyOptions.MinimumSameSiteMode
**Issue**: The property `MinimumSameSiteMode` doesn't exist in .NET 10.0 - this is an incorrect API name.

**Solution**: Change to `MinimumSameSite = SameSiteMode.Lax`.

```csharp
// Before (incorrect):
app.UseCookiePolicy(new CookiePolicyOptions { MinimumSameSiteMode = SameSiteMode.Lax });

// After (correct):
app.UseCookiePolicy(new CookiePolicyOptions { MinimumSameSite = SameSiteMode.Lax });
```

---

### Problem 4: Missing Using Directives for Cookie Authentication
**Issue**: `HttpContext.SignOutAsync` requires the cookie authentication extension method, which needs proper using directives.

**Solution**: Add `using Microsoft.AspNetCore.Authentication.Cookies;` at the top of the file (already present).

---

### Problem 5: ClaimsPrincipal.GetTokenId() Method Doesn't Exist
**Issue**: The method `GetTokenId()` doesn't exist on `ClaimsPrincipal`. This is a non-existent API from Microsoft.Identity.Web.

**Solution**: Extract token ID manually from JWT claims using the `Microsoft.IdentityModel.JsonWebTokens` library:

```csharp
using Microsoft.IdentityModel.JsonWebTokens;

// In your OAuth callback handler:
var jwtToken = context.User.GetJwtToken(); // Get the raw JWT token string
var tokenHandler = new JwtSecurityTokenHandler();
var jwtTokenReader = new JwtSecurityToken(tokenHandler.ReadJwtToken(jwtToken));
string tokenId = jwtTokenReader.Claims.First(c => c.Type == "tid").Value;
```

Or alternatively, use the `sub` claim directly as the user identifier since Google OAuth uses it.

---

### Problem 6: Async Endpoint Return Type Mismatch
**Issue**: Some async endpoints (like `/api/auth/login`) return `IResult` but are declared with `async` keyword, causing a type mismatch between `Task<IResult>` and expected return types.

**Solution**: Either remove the `async` keyword from endpoints that don't perform async operations, or ensure all async methods properly await their internal async calls before returning.

For the login endpoint specifically:
```csharp
// Before (incorrect):
app.MapPost("/api/auth/login", async (HttpContext context) => { ... return Results.NoContent(); })

// After (correct - sync version since no actual async operations needed after SignOutAsync):
app.MapPost("/api/auth/login", (HttpContext context) => 
{
    // Perform all operations synchronously
    // ...
    context.Response.Redirect(redirectUrl);
    return Results.NoContent();
})
```

---

### Problem 7: GetEvents Method Scope Issue
**Issue**: The synchronous `GetEvents()` method is called inside the async `SaveEvent()` before it's defined (since types are at end of file).

**Solution**: Rename the sync method to `GetEventsAsync()` and update all callers. This also aligns with the async nature of the rest of the code.

---

### Problem 8: Task Return Type Inconsistency
**Issue**: Some methods return nullable `Task<T?>` while others return non-nullable `List<Event>`, causing issues with null coalescing operator (`??`).

**Solution**: Make all storage methods consistently async and return proper types:
```csharp
public async Task<List<Event>> GetAllEvents() => await GetEventsAsync() ?? new List<Event>();
```

---

### Recommended Implementation Order (After Fixing Program.cs):

1. **Fix Program.cs structure** - Move type definitions to end, fix API method signatures
2. **Create Data folder** - `EventCalendar.API/Data/` directory
3. **Implement IFileLock interface** in `Data/IFileLock.cs`
4. **Implement FileLock<T> class** in `Data/FileLock.cs`  
5. **Update Program.cs** to use the new storage layer
6. **Create AuthController** (or keep endpoints in Program.cs)
7. **Create frontend AuthService.ts**
8. **Create LoginPage.tsx**
9. **Update App.tsx** with protected routes

---

## Updated Checklist Status

### Phase 1: Backend Setup ✅
- [x] Install `Microsoft.Identity.Web` package for Google OAuth
- [x] Configure Google OAuth in Program.cs (add authentication builder) - DONE
- [x] Create Data folder structure (`Data/`) - DONE
- [x] Implement `IFileLock` interface and implementation - DONE
- [ ] Implement `JsonStorage<T>` generic storage wrapper with file locking

### Phase 2: Authentication Endpoints  
- [ ] Add `[Authorize]` attribute to protected endpoints
- [ ] Create `/api/auth/login` endpoint for OAuth callback
- [ ] Create `/api/auth/me` endpoint to get current user info
- [ ] Create `/api/users` endpoint (list all users)

### Phase 3: Event Endpoints with User Filtering
- [x] EventsController created with basic CRUD operations
- [ ] Update `GET /api/events` to accept and filter by `userId` query parameter
- [ ] Add `POST /api/events` for creating events (requires authentication)
- [ ] Add `PUT/DELETE /api/events/{id}` for updating events (owner-only)
- [ ] Add `DELETE /api/events/{id}` for deleting events (owner-only)

### Phase 4: Frontend Authentication
- [ ] Create `AuthService.ts` singleton service
- [ ] Implement login flow with Google OAuth redirect
- [ ] Store JWT token in cookie/header after successful authentication
- [ ] Create logout functionality
- [ ] Add auth state to React context or use static service

### Phase 5: Frontend UI Components
- [ ] Create `LoginPage.tsx` with "Login with Google" button
- [ ] Update `App.tsx` to conditionally render based on auth state
- [ ] Wrap calendar in `<ProtectedRoute>` component
- [ ] Update event forms to show current user's name/email

### Phase 6: Testing & Refinement
- [ ] Test login/logout flow
- [ ] Verify users can only see their own events
- [ ] Test concurrent file access (file locking)
- [ ] Add error handling for auth failures
- [ ] Update README with setup instructions

---

## Next Steps

1. **Create `JsonStorage<T>` generic storage wrapper** in the Data folder
2. **Update EventsController** to support user filtering and owner-only operations
3. **Create AuthController.cs** with login, me, and users endpoints
4. **Create frontend AuthService.ts** for handling authentication state
5. **Create LoginPage.tsx** component
6. **Update App.tsx** with protected routes logic
7. **Add ProtectedRoute component** (or implement inline)
8. **Test the complete flow**

---

## Current Completed Work Summary

### Backend API:
- ✅ Program.cs refactored to use modern .NET 10 WebApplication pattern
- ✅ EventStore.cs created with file-based event storage and FileLock integration
- ✅ EventsController.cs created with CRUD operations (GET, POST, PUT, DELETE)
- ✅ Microsoft.Identity.Web configured for Google OAuth authentication

### Frontend:
- ⏳ AuthService.ts - NOT YET CREATED
- ⏳ LoginPage.tsx - NOT YET CREATED  
- ⏳ ProtectedRoute logic - NOT YET IMPLEMENTED
- ⏳ Event filtering by userId - NOT YET IMPLEMENTED

---

Ready to proceed with creating `JsonStorage<T>` generic storage wrapper? This will provide a reusable base for both user and event storage.

Would you like me to start fixing Program.cs now?
