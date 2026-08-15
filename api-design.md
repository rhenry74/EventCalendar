# API Design Specification

## Base Route Prefix
All API controllers will be prefixed with `/api` and versioned as `v1`.

## Authentication Controllers
- **External Login** - `POST /api/auth/google/external-login`
  - Accepts a JSON payload containing the Google ID token or authorization code.
  - Validates the token with Google’s OAuth endpoint.
  - Creates a signed JWT containing `{ sub: googleId, email, name, role: "user" }`.
  - Returns the JWT in the response body and sets an `Authorization` header.

- **Callback** - `GET /api/auth/google/callback`
  - Handles the redirect from Google’s consent screen.
  - Exchanges the authorization code for an ID token.
  - Generates the same JWT as above and returns it.

- **Logout** - `POST /api/auth/logout`
  - Invalidates the JWT (e.g., adds it to a deny‑list) and clears client‑side storage.

All auth endpoints are `[AllowAnonymous]`. The JWT generation logic lives in a shared `JwtService` class.

## Event Controllers
- **Get Events** - `GET /api/events`
  - `[Authorize]` attribute requires a valid JWT.
  - Returns a list of events for the authenticated user, filtered by `IsPublic` or ownership.

- **Get Event Details** - `GET /api/events/{id}`
  - `[Authorize]`.
  - Returns the full event object if the user is the owner or has been granted access.

- **Create Event** - `POST /api/events`
  - `[Authorize]`.
  - Accepts an `EventDto` payload.
  - Sets `OwnerId` to the authenticated user’s `Id`.
  - Persists the event to `events.json` using the file‑locking write pattern.

- **Update Event** - `PUT /api/events/{id}`
  - `[Authorize]`.
  - Allows modifications only if the requester is the event owner or has admin rights.

- **Delete Event** - `DELETE /api/events/{id}`
  - `[Authorize]`.
  - Removes the event from `events.json`.

- **Share Event** - `POST /api/events/{id}/share`
  - `[Authorize]`.
  - Payload includes `userId` and `permissionLevel` (`Read`, `Write`, `Admin`).
  - Updates the event’s access control list (stored alongside the event object).

All event endpoints are `[Authorize]`.

## User Controllers
- **Get Current User** - `GET /api/users/me`
  - `[Authorize]`.
  - Returns the authenticated user’s profile (Id, DisplayName, Email, AvatarUrl).

- **Update Current User** - `PUT /api/users/me`
  - `[Authorize]`.
  - Accepts a partial `UserUpdateRequest` and updates the profile fields.

- **Get User Profile** - `GET /api/users/{userId}`
  - `[Authorize]` (optional role check).
  - Returns another user’s public profile information.

## Global Authorization
- A custom `AuthorizationMiddleware` will validate the JWT on every request.
- If validation fails, a `401 Unauthorized` response is returned.
- The middleware extracts the `sub` claim to identify the user ID for subsequent authorization checks.

## File Storage
- Event data is stored in `Data/events.json`.
- User data is stored in `Data/users.json`.
- Writes use a retry‑based lock‑file strategy to prevent corruption while allowing concurrent reads.

This design outlines the routes, authentication flow, and the separation of controllers to keep `Program.cs` minimal. Implementation will follow by adding the corresponding controller classes in the `Controllers` folder.