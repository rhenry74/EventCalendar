# Multi-User Update Plan

1. [x] Determine authentication implementation / desired auth method (Google OAuth 2.0)
2. [x] Identify required multi-user features (public vs private events, sharing, permissions)
- Public events: visible to all users; can be discovered and joined by anyone.
- Private events: invitation-only; accessible only to explicitly invited users.
- Event sharing & permissions: ability to share events with specific users or groups; set read/write permissions; optional expiration of shared access.
- Additional considerations: event categories, recurring events, notification preferences, and access logs.
3. [x] Define data model extensions (User, Event, file mapping, ownership)
4. [x] Design API changes (auth routes, protected event endpoints, separate controller files)
5. [x] Plan file locking strategy (retry with lock file to avoid corruption)
6. [x] Scaffold backend project structure (Controllers, Data, Services)
7. [x] Implement Google OAuth middleware and JWT creation
8. [x] Create frontend Google Sign‑In UI component
9. [x] Integrate token storage (localStorage) and API authentication headers
10. [ ] Refactor data layer to use JSON files with LINQ and file‑locking writes
11. [ ] Write initial seed data and JSON files (users.json, events.json)
12. [ ] Add unit and integration tests for auth and event filtering
13. [ ] Test concurrent access and locking behavior
14. [ ] Prepare deployment configuration (environment variables, secrets)