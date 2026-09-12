import React from 'react';
import type { Category, Event } from '../types';
import { IconButton, Box, Typography } from '@mui/material';
import DeleteIcon from '@mui/icons-material/Delete';
import EventIcon from '@mui/icons-material/Event';
import FenceIcon from '@mui/icons-material/Fence';
import PublicIcon from '@mui/icons-material/Public';
import type { Theme } from '@mui/material/styles';
import { isTimedEventValue } from '../dateUtils';
const hasTime = isTimedEventValue;
const timeLabel = (value: string) => new Date(value).toLocaleTimeString(undefined, {
  hour: 'numeric', minute: '2-digit'
});

type EventDayStatus = 'single' | 'start' | 'continued' | 'last';

const formatEventTiming = (event: Event, status: EventDayStatus) => {
  if (status === 'continued') return 'Continued';
  if (status === 'start') return `Starts${hasTime(event.date) ? ` ${timeLabel(event.date)}` : ''}`;
  if (status === 'last') return `Last day${event.endDate && hasTime(event.endDate) ? ` · Ends ${timeLabel(event.endDate)}` : ''}`;
  if (!hasTime(event.date) && (!event.endDate || !hasTime(event.endDate))) return 'All day';
  if (event.endDate && hasTime(event.date) && hasTime(event.endDate)) {
    return `${timeLabel(event.date)} – ${timeLabel(event.endDate)}`;
  }
  return hasTime(event.date) ? timeLabel(event.date) : `Ends ${timeLabel(event.endDate!)}`;
};

interface EventCardProps {
  event: Event;
  categories: Category[];
  onDelete: (id: string) => void;
  onEdit: () => void;
  dayStatus?: EventDayStatus;
  theme?: Theme;
}

const EventCard: React.FC<EventCardProps> = ({ event, categories, onDelete, onEdit, dayStatus = 'single', theme }) => {
  const category = categories.find(item => item.name === event.category);
  const location = event.location?.trim();
  const primaryColor = category?.primaryColor || theme?.palette.primary.main || '#60a5fa';
  const secondaryColor = category?.secondaryColor || theme?.palette.primary.light || '#9ca3af';
  const categoryIcon = category?.icon || '📌';

  return (
    <Box 
      sx={{ 
        width: '100%',
        minWidth: 0,
        '&:hover': { backgroundColor: theme?.palette.action.hover || 'rgba(96, 165, 250, 0.08)' }
      }}
    >
      <Box 
        sx={{ 
          display: 'grid', 
          gridTemplateColumns: 'auto minmax(0, 1fr) auto',
          gap: 0,
          width: '100%',
          p: 0,
          borderLeft: `4px solid ${secondaryColor}`,
          backgroundColor: primaryColor,
          borderRadius: 1,
          '&:hover': {
            backgroundColor: primaryColor,
            filter: 'brightness(1.15)'
          }
        }}
      >
        {/* Only an event owner can edit or delete it. */}
        <Box sx={{ gridColumn: '1 / 2', textAlign: 'left'}}>
          {event.isOwner &&
          <IconButton 
            size="small" 
            onClick={(e) => { e.stopPropagation(); onEdit(); }}
            sx={{ color: secondaryColor, '&:hover': { backgroundColor: `${secondaryColor}35` } }}
          >
            <EventIcon fontSize="small" />
          </IconButton>
          }
        </Box>

        {/* Title - Row 1, Column 2 */}
        <Box sx={{ gridColumn: '2 / 3', minWidth: 0, display: 'flex', alignItems: 'center', justifyContent: 'center', textAlign: 'center', py: 0.5, overflowWrap: 'anywhere', wordBreak: 'break-word', whiteSpace: 'normal', fontSize: { xs: '0.68rem', sm: '0.75rem' }, lineHeight: 1.2, fontWeight: 600, color: '#fff', cursor: 'default' }}>
          {event.title}
        </Box>

        {/* Delete button - Row 1, Column 3 */}
        <Box sx={{ gridColumn: '3 / -1', textAlign: 'right'}}>
          {event.isOwner &&
          <IconButton 
            size="small" 
            onClick={(e) => { e.stopPropagation(); onDelete(event.id); }}
            sx={{ color: secondaryColor, '&:hover': { backgroundColor: `${secondaryColor}35` } }}
          >
            <DeleteIcon fontSize="small" />
          </IconButton>
          }
        </Box>

        {/* Category - Spans all 3 columns */}
        <Box 
          sx={{ 
            gridColumn: 'span 3',
            textAlign: 'center',
            py: 0.75,
            fontSize: '0.6rem',
            fontWeight: 600,
            backgroundColor: secondaryColor,
            color: '#071a2f',
            px: 1,
            borderRadius: 1,
            cursor: 'default'
          }}
        >
          <span aria-hidden="true">{categoryIcon}</span> {event.category || 'General'}
          {' · '}
          {event.isPublic ? (
            <PublicIcon fontSize="inherit" titleAccess="Public" aria-label="Public" />
          ) : (
            <FenceIcon fontSize="inherit" titleAccess="Private" aria-label="Private" />
          )}
        </Box>

        {/* Description - Spans all 3 columns */}
        <Box sx={{ gridColumn: 'span 3', minWidth: 0, overflowWrap: 'anywhere', cursor: 'default' }}>
          <Typography variant="caption" sx={{ color: 'rgba(255,255,255,0.9)' }}>{event.description}</Typography>
        </Box>

        {/* Location - Spans all 3 columns */}
        {location && (
          <Box sx={{ gridColumn: 'span 3', minWidth: 0, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap', textAlign: 'center', fontSize: { xs: '0.7rem', sm: '0.6rem' }, color: secondaryColor, cursor: 'default' }}>
            {location}
          </Box>
        )}

        {/* Date - Spans all 3 columns */}
        <Box sx={{ gridColumn: 'span 3', textAlign: 'center', fontSize: { xs: '0.7rem', sm: '0.6rem' }, color: secondaryColor, cursor: 'default' }}>
          {formatEventTiming(event, dayStatus)}
        </Box>
      </Box>
    </Box>
  );
};

export default EventCard;
