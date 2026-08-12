# EventCalendar - Architecture Documentation

## Overview

EventCalendar is a React application built with TypeScript and Vite that displays events on a calendar grid view. The app allows users to add, edit, and delete events.

## Tech Stack

- **React**: UI framework for component-based architecture
- **TypeScript**: Type safety throughout the application
- **Vite**: Fast development server and build tool
- **@mui/material**: Material-UI components for styling and dialog/inputs
- **MUI Date Pickers**: For date/time input in event dialogs (optional, currently using native datetime-local)

## Project Structure

```
EventCalendar/
├── src/
│   ├── components/
│   │   ├── Calendar.tsx       # Main calendar grid component
│   │   ├── EventCard.tsx      # Individual event display card
│   │   └── EventDialog.tsx    # Add/Edit event modal dialog
│   ├── types.ts               # TypeScript interfaces
│   ├── mockData.ts            # Sample event data
│   ├── App.tsx                # Main app component (state management)
│   ├── App.css                # Global styles
│   └── index.*                # Entry points
├── public/                    # Static assets
└── package.json               # Dependencies and scripts
```

## Component Architecture

### 1. App.tsx - Root Container & State Manager

**Responsibilities:**
- Holds the main state: `events` array (list of all events)
- Provides CRUD operations via callback functions to child components
- Manages dialog visibility state

**State:**
```typescript
const [events, setEvents] = useState<Event[]>(mockEvents)
const [isDialogOpen, setIsDialogOpen] = useState(false)
```

**Key Handlers:**
- `addEvent(event)`: Adds new event to the list
- `deleteEvent(id)`: Removes event by ID
- `handleOpenDialog(event?)`: Opens dialog with optional pre-fill for edit
- `handleCloseDialog()`: Closes dialog

**Data Flow:**
```
User Action (click Add/Edit) → handleOpenDialog → EventDialog opens
User saves in EventDialog → App.handleSaveEvent → updates state
App re-renders Calendar with new events list
```

### 2. Calendar.tsx - Calendar Grid Display

**Responsibilities:**
- Renders the current month's calendar grid
- Filters events to display on correct dates
- Provides navigation (Prev/Next month)
- Triggers Add Event button

**Key Logic:**

```typescript
const calendarDays = useMemo(() => {
  // Calculate days in current month
  const daysInMonth = new Date(year, month + 1, 0).getDate();
  
  for (let d = 1; d <= daysInMonth; d++) {
    const date = new Date(year, month, d);
    
    // Filter events for this specific day
    // Using ISO string comparison to handle timezone differences
    const currentDayISO = date.toISOString().split('T')[0];
    const dayEvents = events.filter(e => 
      e.date.toISOString().split('T')[0] === currentDayISO
    );
    
    days.push({ day: d, date, events: dayEvents });
  }
}, [currentMonth, events]);
```

**Important Design Decision:**
The filtering uses `toISOString().split('T')[0]` instead of `.getMonth()/.getDate()` to avoid timezone-related bugs. Date strings like "2026-08-15" are compared as-is in UTC, ensuring consistency regardless of browser timezone settings.

### 3. EventDialog.tsx - Event Form Modal

**Props Interface:**
```typescript
interface EventDialogProps {
  open: boolean;                // Controls dialog visibility
  onClose: () => void;          // Close callback
  initialEvent?: Event;         // Pre-fill data for editing
  onSave: (event: Event) => void; // Save handler from parent
}
```

**State:**
- `title`, `description`: Text fields
- `location`: Optional text field
- `category`: Dropdown/category selector
- `date`: DateTime picker state

**Key Logic:**

**Edit Mode Detection:**
```typescript
const [date, setDate] = useState<Date>(initialEvent?.date || new Date());

React.useEffect(() => {
  if (initialEvent) {
    // Populate form with existing event data
    setTitle(initialEvent.title);
    setDescription(initialEvent.description);
    setLocation(initialEvent.location || '');
    setCategory(initialEvent.category || 'General');
    setDate(new Date(initialEvent.date));
  } else {
    // New event: empty fields, today's date
    setTitle('');
    setDescription('');
    setLocation('');
    setCategory('General');
    setDate(new Date());
  }
}, [initialEvent, open]);
```

**Save Handler with Validation:**
```typescript
const handleSave = () => {
  if (!title.trim()) {
    alert("Title is required");
    return; // Prevent save on invalid form
  }
  
  onSave({
    id: initialEvent?.id || Math.random().toString(36).substr(2, 9),
    title: title.trim(),
    description: description.trim(),
    location: location.trim(),
    category,
    date: date.toISOString(), // Store as ISO string for consistent comparison
  });
  onClose();
};
```

**Design Decisions:**
- Generates new UUID-like IDs for new events
- Stores dates as ISO strings to maintain consistency with calendar filtering
- Trims whitespace from text fields to avoid empty values
- Shows validation alert if title is empty on save

### 4. EventCard.tsx - Individual Event Display

**Props Interface:**
```typescript
interface EventCardProps {
  event: Event;
  onDelete: (id: string) => void;
  onEdit: () => void;
}
```

**Responsibilities:**
- Renders event details (title, location, category, date)
- Provides delete and edit action buttons
- Passes callbacks back to parent for state updates

## Data Model

### Event Interface (`types.ts`)

```typescript
interface Event {
  id: string;           // Unique identifier (UUID-like or existing ID)
  title: string;        // Required - event name
  description: string;  // Optional - event details
  date: Date;           // Event datetime (stored as ISO string in state)
  location?: string;    // Optional - venue/location
  category?: string;    // Optional - classification
}
```

## Data Flow Summary

### Adding a New Event
1. User clicks "Add Event" button → `handleOpenDialog()` opens dialog
2. Dialog shows empty form, today's date pre-filled
3. User fills form, clicks Save
4. `EventDialog.handleSave()` generates new ID, calls parent `onSave`
5. App's `handleSaveEvent()` adds event via `addEvent()`
6. State updates → Calendar re-renders with new event

### Editing an Existing Event
1. User clicks Edit on event card → `handleOpenDialog(event)` passes existing event
2. Dialog shows pre-filled data from `initialEvent` prop
3. User modifies any fields, clicks Save
4. `EventDialog.handleSave()` calls parent `onSave` with updated data
5. App's `handleSaveEvent()` updates event via `delete+add` or map
6. State updates → Calendar re-renders

### Deleting an Event
1. User clicks delete on event card
2. `Calendar.onDeleteEvent(id)` called
3. App's `deleteEvent(id)` filters out the event
4. State updates → Calendar re-renders without event

## Key Patterns Used

### 1. Props-based Communication
Parent components pass callbacks down to children (lifted state pattern).

### 2. useMemo for Performance
Calendar uses `useMemo` to efficiently recalculate day data only when month or events change.

### 3. Controlled Components
All form inputs use React state (`useState`) making them fully controlled, preventing unintended bugs.

### 4. ISO String Date Handling
Dates are stored and compared as ISO strings (e.g., "2026-08-15T12:00:00") to ensure timezone-independent comparisons.

## Future Enhancements (Optional)

- Replace native datetime-local with MUI's `DatePicker` component for better UX
- Add event categories filter dropdown
- Implement drag-and-drop event rescheduling
- Persist events to localStorage or backend API
- Add confirmation dialog before deleting events
- Support recurring events
