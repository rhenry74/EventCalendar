# Multi-User Features Specification

## Event Visibility
- **Public Events**: Visible to all authenticated users. Any user can view and join these events.
- **Private Events**: Invitation‑only. Only users explicitly invited by the event owner can view or join.

## Event Sharing & Permissions
- **Share Events**: Ability to share an event with specific users or groups.
- **Permission Levels**:
  - *Read*: Invited users can view event details.
  - *Write*: Invited users can edit or delete the event.
  - *Admin*: Full control over the event lifecycle (create, modify, delete, and manage participants).
- **Expiration**: Optionally set an expiration date for shared access, after which permissions are revoked.

## Additional Features
- **Event Categories/Tags**: Organize events into categories or tags for easier discovery.
- **Recurring Events**: Support for events that repeat on a schedule (daily, weekly, custom).
- **Notifications**: In‑app or email notifications for event changes, invitations, reminders, etc.
- **Access Logs**: Track which users have viewed or modified an event.

## User Management
- **User Profiles**: Store basic profile information (display name, avatar URL, email).
- **Invitation System**: Mechanism to invite users via email addresses or shareable links.

These features will be persisted using JSON files (one file per entity) with LINQ queries for access. File writes will use a retry‑based lock‑file strategy to prevent corruption while still allowing concurrent reads.