# EventCalendar

A simple React-based calendar application that displays events across different dates with a .NET API backend for event management.

## Features

- **Dynamic Calendar Grid**: Displays a month-by-month view of dates.
- **Month Navigation**: Easily switch between previous and next months using navigation controls.
- **Event Display**: Renders event cards on specific dates, showing information such as titles, locations, and categories.
- **CRUD Operations**: Create, read, update, and delete events via REST API.
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
├── public/events.json    # Events data file (shared with API)
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

   The API will be available at `http://localhost:5000` with endpoints:
   - `GET /api/events` - List all events
   - `POST /api/events` - Create new event
   - `PUT /api/events/{id}` - Update existing event
   - `DELETE /api/events/{id}` - Delete event
   - `GET /health` - Health check

3. Start the frontend development server (new terminal):
   ```bash
   npm run dev
   ```

The application will be available at `http://localhost:5173`. The frontend connects to the API via CORS-enabled endpoints at port 5000.

### Architecture Note

- **Frontend (Port 5173)**: Vite development server serving React app
- **API Backend (Port 5000)**: .NET minimal API handling event CRUD operations
- **Shared Data**: Both frontend and API read/write to `public/events.json` for persistence
