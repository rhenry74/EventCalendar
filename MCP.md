# EventCalendar MCP server

The API exposes a remote, streamable HTTP MCP server at `/mcp` with these
tools:

- `list_categories`
- `search_events`
- `get_event`
- `create_event`
- `update_event`
- `delete_event`

Each Google-signed-in user can issue a personal MCP bearer token by clicking the
**MCP** chip beside their name in the web app. The chip calls
`POST /api/mcp/token`, copies the returned token to the clipboard, and displays
a confirmation toast.

Use the token as:

```text
Authorization: Bearer <token>
```

Tokens are 64-character hexadecimal values generated from 32 random bytes. The
API stores only an in-memory token-to-Google-subject mapping. Tokens disappear
when the API restarts, and a user can generate a replacement at any time.

The resolved Google subject controls all permissions:

- read tools see the user's private events and all public events;
- created events are owned by that user;
- updates and deletes are limited to that user's events;
- unknown or restarted-process tokens return `401 Unauthorized`.

Call `list_categories` before creating or updating events. Use the returned
category `name` exactly; `search_events` accepts the same name as its optional
`category` filter.

For `update_event`, title and date are required. Omit optional fields to keep
their current values.

The MCP endpoint is:

```text
https://<your-app>.azurewebsites.net/mcp
```

Use HTTPS in deployment. External clients must support a manually supplied
bearer header. Clients that require interactive OAuth instead need an OAuth
adapter in front of this endpoint.

See the full setup, verification, and OpenAI client instructions in
[`README.md`](README.md).
