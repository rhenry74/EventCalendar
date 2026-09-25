# EventCalendar

A simple React-based calendar application that displays events across different dates with a .NET API backend for event management.

## Features

- **Dynamic Calendar Grid**: Displays a month-by-month view of dates.
- **Month Navigation**: Easily switch between previous and next months using navigation controls.
- **Event Display**: Renders event cards on specific dates, showing information such as titles, locations, and categories.
- **Google sign-in**: Events are owned by their creator; owners can make events public.
- **Access control**: Users can only edit or delete their own events.
- **Category Filtering**: Events organized by category (Tech, Entertainment, Art, etc.).
- **Interactive Event Dialog**: Add new events or edit existing ones with a form dialog.

## Tech Stack

- **React**: For building the user interface.
- **TypeScript**: For type safety and better developer experience.
- **Vite**: For a fast and modern development environment.
- **Oxlint**: For fast linting.
- **.NET 10**: Backend API using minimal APIs.

## Project Structure

```
EventCalendar/
├── src/                  # React frontend
│   ├── components/       # React components (Calendar, EventCard, EventDialog)
│   ├── types.ts          # TypeScript type definitions
│   └── App.css           # Frontend styles
├── EventCalendar.API/    # .NET backend API
│   ├── Program.cs        # API routes and CORS configuration
│   └── appsettings.json  # API configuration
├── public/events.json    # Legacy seed events file
├── public/categories.json # Runtime category definitions
└── README.md             # This file
```

## Getting Started - Frontend Only

To run the application locally without the API:

1. Install dependencies:
   ```bash
   npm install
   ```

2. Start the development server:
   ```bash
   npm run dev
   ```

The application will be available at `http://localhost:5173`.

## Getting Started - With API Backend

To run the full application with the .NET API backend:

### Prerequisites
- Node.js (v18+) for frontend
- .NET 10 SDK for backend

### Steps

1. Install frontend dependencies:
   ```bash
   npm install
   ```

2. Build and run the .NET API in a separate terminal:
   ```bash
   cd EventCalendar.API
   dotnet build
   dotnet run
   ```

   The API will be available at `http://localhost:5115` with endpoints:
   - `GET /api/auth/login` - Start Google sign-in
   - `GET /api/events` - List public events plus the signed-in user's private events
   - `POST /api/events` - Create new event
   - `PUT /api/events/{id}` - Update existing event
   - `DELETE /api/events/{id}` - Delete event
   - `GET /api/events/backup` - Download the full event store for the backup account
   - `POST /api/events/backup` - Replace the full event store from a JSON backup
   - `POST /api/mcp/token` - Issue an MCP token for the signed-in Google user
   - `POST /mcp` - Streamable HTTP MCP endpoint
   - `GET /health` - Health check

3. Start the frontend development server (new terminal):
   ```bash
   npm run dev
   ```

The application will be available at `http://localhost:5173`. The frontend connects to the API via CORS-enabled endpoints at port 5115.

### Google sign-in setup

1. In Google Cloud Console, create an OAuth 2.0 **Web application** client.
2. Add `http://localhost:5115/signin-google` as an authorized redirect URI.
3. Store the client values outside Git using .NET user secrets:

   ```powershell
   cd EventCalendar.API
   dotnet user-secrets set "OAuth:ClientId" "your-client-id"
   dotnet user-secrets set "OAuth:ClientSecret" "your-client-secret"
   ```

4. Run the API and frontend, then use **Continue with Google**. For deployment, register the production callback URL and provide these values through the host's secret or environment-variable system (`OAuth__ClientId` and `OAuth__ClientSecret`).

## MCP server setup

The API includes a remote, streamable HTTP MCP server at `/mcp`. An external AI
client can use it to search, create, update, and delete calendar events. The
available tools are:

- `list_categories` — return the exact category names and display metadata
  available to the application.
- `search_events` — read events visible to the MCP identity.
- `get_event` — read one visible event by ID.
- `create_event` — create an event owned by the MCP identity.
- `update_event` — update an event owned by the MCP identity.
- `delete_event` — permanently delete an event owned by the MCP identity.

Call `list_categories` before creating or updating an event and use the exact
returned `name` value for its `category`. `search_events` also accepts that
exact name through its optional `category` filter.

When updating an event, title and date are required; any omitted optional field
keeps its existing value.

The browser UI continues to use Google cookie authentication. MCP clients do
not have that browser cookie, so each signed-in user can issue a separate MCP
bearer token for their own account. Tokens are held in memory by the API
process and are lost when the process restarts; users can issue a replacement
from the UI at any time.

### Issue a user token

1. Sign in with Google.
2. Click the **MCP** chip beside your name in the application header.
3. The browser calls `POST /api/mcp/token`, receives a new token, copies it to
   the clipboard, and shows a **Token copied** notification.

The token response is:

```json
{
  "token": "<64-character-hex-token>",
  "userId": "<google-subject-id>"
}
```

The token is generated from 32 bytes of cryptographically secure random data
and hex-encoded. It is not persisted in the event store or database. A token
is accepted only while the current API process has its in-memory mapping.

### How token ownership works

An MCP request must include:

```text
Authorization: Bearer <token>
```

The MCP middleware resolves that token to the Google subject ID that issued it,
then runs all tool calls as that user. Therefore:

- `search_events` and `get_event` return that user's private events plus public
  events.
- `create_event` assigns the resolved user as owner.
- `update_event` and `delete_event` work only for events owned by that user.
- A missing, expired-after-restart, or unknown token returns `401 Unauthorized`.

No `Mcp__ApiKey`, `Mcp__UserId`, or `Mcp__UserName` application settings are
required anymore. Google OAuth settings (`OAuth:ClientId` and
`OAuth:ClientSecret`) are still required for browser sign-in.

### Find the Google user ID

1. Start the API and frontend and sign in with Google.
2. Open `http://localhost:5115/api/auth/me` in the same browser session.
3. Copy the value of the `id` property. It will look like a long numeric Google
   subject ID. Do not substitute `rhenry74@gmail.com`; the email is a display
   value while the numeric subject ID is the ownership key used by the API.

### Configure Azure App Service

The token is generated by the deployed UI, so there is no MCP secret to add to
Azure App Service settings. Configure the normal Google OAuth settings as
before, deploy the API and frontend, then:

1. Open the Web App.
2. Go to **Settings → Environment variables → App settings**.
3. Confirm the Google settings `OAuth__ClientId` and `OAuth__ClientSecret` are
   present.
4. Click **Apply**, then restart the Web App.

### Build the Azure deployment ZIP

Run these commands from the repository root. The package contains the
published API, the compiled frontend, the runtime category file, and the
current `EventCalendar.API/Data/events.json` store:

```powershell
npm run build

$stage = Join-Path (Get-Location) '.azure-package'
$zip = Join-Path (Get-Location) 'EventCalendar-azure-deploy.zip'
if (Test-Path -LiteralPath $stage) { Remove-Item -LiteralPath $stage -Recurse -Force }

dotnet publish EventCalendar.API/EventCalendar.API.csproj `
  --configuration Release --output $stage

$wwwroot = Join-Path $stage 'wwwroot'
if (Test-Path -LiteralPath $wwwroot) { Remove-Item -LiteralPath $wwwroot -Recurse -Force }
New-Item -ItemType Directory -Path $wwwroot | Out-Null
Copy-Item -Path (Join-Path (Get-Location) 'dist\*') -Destination $wwwroot -Recurse -Force

$developmentSettings = Join-Path $stage 'appsettings.Development.json'
if (Test-Path -LiteralPath $developmentSettings) {
  Remove-Item -LiteralPath $developmentSettings -Force
}

$data = Join-Path $stage 'Data'
if (Test-Path -LiteralPath $data) { Remove-Item -LiteralPath $data -Recurse -Force }
New-Item -ItemType Directory -Path $data | Out-Null
Copy-Item -LiteralPath (Join-Path (Get-Location) 'EventCalendar.API\Data\events.json') `
  -Destination (Join-Path $data 'events.json') -Force

if (Test-Path -LiteralPath $zip) { Remove-Item -LiteralPath $zip -Force }
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip -CompressionLevel Optimal
```

`EventCalendar.API/Data/events.json` is ignored by Git but is intentionally
included in this deployment package. It may contain private events; review it
before sharing the ZIP. Configure these Azure App Service settings before
launching the app:

- `OAuth__ClientId`
- `OAuth__ClientSecret`
- `Frontend__Origin` = `https://<your-app-name>.azurewebsites.net`

You can deploy the resulting archive from the Azure Portal or with Azure CLI:

```powershell
az webapp deploy --resource-group <resource-group> --name <app-name> `
  --src-path .\EventCalendar-azure-deploy.zip --type zip
```

The MCP URL is:

```text
https://<your-app-name>.azurewebsites.net/mcp
```

Use HTTPS for any deployed MCP endpoint. If a token is exposed, sign out or
restart the API to clear in-memory tokens, then issue a new token from the UI.

### Event backup and restore

The **BACKUP** and **UPLOAD** chips are shown only to the hard-coded backup
account `rhenry74@gmail.com`. BACKUP downloads the complete `events.json` data
store. UPLOAD replaces the server's event store after confirmation, so keep a
known-good backup before restoring a file.

### Verify the endpoint

The endpoint requires a user token and an `Accept` header containing
`application/json, text/event-stream`:

```powershell
$headers = @{ Authorization = "Bearer <paste-token>"; Accept = "application/json, text/event-stream" }
$body = @{
  jsonrpc = "2.0"
  id = 1
  method = "initialize"
  params = @{
    protocolVersion = "2025-11-25"
    capabilities = @{}
    clientInfo = @{ name = "manual-test"; version = "1.0" }
  }
} | ConvertTo-Json -Depth 10

Invoke-WebRequest -Uri "http://localhost:5115/mcp" -Method Post `
  -Headers $headers -ContentType "application/json" -Body $body
```

After a successful `initialize` response, send a JSON-RPC `tools/list` request
to confirm that all five tools are advertised. A `401` means the bearer token is
missing, incorrect, or was cleared by an API restart.

### Connect an OpenAI API client

Configure a remote MCP tool with the HTTPS server URL and the bearer header. Keep
approval enabled for `create_event`, `update_event`, and `delete_event`; leave
`search_events` and `get_event` read-only. The server also publishes MCP
read-only and destructive annotations for clients that use them.

The current server supports clients that can send a custom bearer header. A
ChatGPT connector or other host that requires an OAuth authorization flow will
need an OAuth layer in front of this endpoint; the MCP token chip provides the
token for clients that support manually supplied bearer credentials.

### Architecture Note

- **Frontend (Port 5173)**: Vite development server serving React app
- **API Backend (Port 5115)**: .NET minimal API handling Google authentication and event authorization
- **Data**: `EventCalendar.API/Data/events.json` stores local events and is ignored by Git. Legacy sample events are imported once as public events. `public/categories.json` is copied into the deployed `wwwroot` and read at runtime by both the UI and MCP category tool.
