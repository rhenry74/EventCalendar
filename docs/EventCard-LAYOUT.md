# Event Card Layout Documentation

## Overview
The EventCard component displays event information in a compact, multi-row card layout using CSS Grid. The card is designed to fit within calendar day cells while maintaining readability and visual hierarchy.

---

## Visual Structure

```
┌─────────────────────────────────────┐
│  [Edit]   Event Title    [Delete]   │  ← Row 1: Header (3 columns)
├─────────────────────────────────────┤
│         Category Badge              │  ← Row 2: Category (spans all 3 cols)
│         Description                 │  ← Row 3: Description (spans all 3 cols)
│         Location                    │  ← Row 4: Location (spans all 3 cols)
│         Date                        │  ← Row 5: Date (spans all 3 cols)
└─────────────────────────────────────┘
```

---

## Grid Configuration

### Main Container
- **Grid Template:** `repeat(3, 1fr)` - Creates 3 equal-width columns
- **Gap:** `0` - No spacing between rows/columns for compact layout
- **Width:** `100%` - Fills the parent container width
- **Padding:** `0` - No outer padding; row height collapses to content size
- **Border Left:** `3px solid [primaryColor]` - Visual accent on left edge
- **Background Color:** Dark theme color (`#020d30`) with hover state (`#04193a`)

---

## Row Details

### Row 1: Header (Action Buttons + Title)
| Column | Content | Alignment | Span | Cursor |
|--------|---------|-----------|------|--------|
| 1 | Edit Icon Button | Left | `gridColumn: '1 / 2'` | Inherits from IconButton hover |
| 2 | Event Title | Centered in column | `gridColumn: '2 / 3'` | `cursor: 'default'` |
| 3 | Delete Icon Button | Right | `gridColumn: '3 / -1'` | Inherits from IconButton hover |

**Notes:**
- Edit button uses `EventIcon` from Material Icons
- Delete button uses `DeleteIcon` from Material Icons
- Both buttons are `IconButton` components with small size
- Click events use `stopPropagation()` to prevent card click from triggering
- Text elements (title, category, description, location, date) have `cursor: 'default'` to prevent browser from showing edit cursor on hover

### Row 2: Category Badge
| Property | Value |
|----------|-------|
| Span | `gridColumn: 'span 3'` - Spans all 3 columns |
| Alignment | Centered horizontally |
| Vertical Padding | `py: 0.75` (12px) for badge prominence |
| Font Size | `0.6rem` |
| Font Weight | `600` (semi-bold) |
| Background | Semi-transparent primary color (`[primaryColor]15`) |
| Text Color | Primary color |
| Corner Radius | `borderRadius: 1` (4px) |
| Cursor | `cursor: 'default'` - Prevents edit cursor on hover |

### Row 3: Description
| Property | Value |
|----------|-------|
| Span | `gridColumn: 'span 3'` - Spans all 3 columns |
| Alignment | Centered horizontally |
| Vertical Padding | None (collapses to content height) |
| Font Size | `0.6rem` |
| Text Color | Secondary color (`#9ca3af`) |
| Cursor | `cursor: 'default'` - Prevents edit cursor on hover |

### Row 4: Location
| Property | Value |
|----------|-------|
| Span | `gridColumn: 'span 3'` - Spans all 3 columns |
| Alignment | Centered horizontally |
| Vertical Padding | None (collapses to content height) |
| Font Size | `0.6rem` |
| Text Color | Secondary color (`#9ca3af`) |
| Default Value | `'TBD'` when location is empty |
| Cursor | `cursor: 'default'` - Prevents edit cursor on hover |

### Row 5: Date
| Property | Value |
|----------|-------|
| Span | `gridColumn: 'span 3'` - Spans all 3 columns |
| Alignment | Centered horizontally |
| Vertical Padding | None (collapses to content height) |
| Font Size | `0.6rem` |
| Text Color | Secondary color (`#9ca3af`) |
| Format | Browser's native `toLocaleDateString()` |
| Cursor | `cursor: 'default'` - Prevents edit cursor on hover |

---

## Styling Details

### Hover State
- **Container:** Background changes from `#020d30` to `#04193a` on hover
- **Buttons:** Background becomes action hover color (`rgba(96, 165, 250, 0.08)`)
- **Text Elements:** Show default arrow cursor (not edit cursor) via `cursor: 'default'`

### Color Variables
```typescript
primaryColor = theme?.palette.primary.main || '#60a5fa'
secondaryColor = theme?.palette.text.secondary || '#9ca3af'
```

---

## Responsive Behavior
- The card maintains its 3-column grid layout regardless of container size
- Row heights collapse naturally based on content (no fixed minimum height)
- Category badge has consistent padding for visual prominence as a "label" row

---

## Usage Example
```tsx
<EventCard 
  event={event}
  onDelete={(id) => handleDelete(id)}
  onEdit={() => handleEdit()}
/>
```

---

## Key Design Principles
1. **Compactness:** No gaps between rows, no container padding - maximizes space efficiency
2. **Visual Hierarchy:** Title is prominent (larger font), category stands out with background color
3. **Content Spanning:** Secondary information (description, location, date) spans full width for readability
4. **Action Accessibility:** Edit and delete buttons are in dedicated columns for easy identification
5. **Cursor Behavior:** Text elements use `cursor: 'default'` to prevent browser from showing edit cursor on hover; interactive buttons show pointer cursor on hover
