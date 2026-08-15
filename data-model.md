# Data Model Specification

## User
- **Id**: Unique identifier (string)
- **GoogleId**: Google OAuth provider user ID (string, unique)
- **DisplayName**: User's display name (string)
- **Email**: Email address (string)
- **AvatarUrl**: URL to user's avatar image (string, optional)
- **CreatedAt**: Timestamp when the user was created (DateTime)

## Event
- **Id**: Unique identifier (string)
- **OwnerId**: Reference to the User who owns the event (string, foreign key)
- **Title**: Event title (string)
- **Description**: Event description (string)
- **Start**: Start time (DateTime)
- **End**: End time (DateTime)
- **IsPublic**: Boolean indicating if the event is public (true) or private (false)
- **Category**: Optional category/tag (string)
- **Recurrence**: Optional recurrence pattern (string)
- **CreatedAt**: Timestamp when the event was created (DateTime)
- **UpdatedAt**: Timestamp when the event was last updated (DateTime)

## File Mapping
- **users.json**: Persistent storage for User objects (one file per entity)
- **events.json**: Persistent storage for Event objects (one file per entity)

## Ownership
- Each Event must have an OwnerId that references a User's Id, ensuring events are tied to a specific user.

This specification will guide the implementation of the JSON-based data layer with LINQ queries and file locking for writes.