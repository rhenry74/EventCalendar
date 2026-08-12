import React from 'react';
import type { Event } from '../types';
import { IconButton, Box, Typography } from '@mui/material';
import DeleteIcon from '@mui/icons-material/Delete';
import EventIcon from '@mui/icons-material/Event';
import type { Theme } from '@mui/material/styles';

interface EventCardProps {
  event: Event;
  onDelete: (id: string) => void;
  onEdit: () => void;
  theme?: Theme;
}

const EventCard: React.FC<EventCardProps> = ({ event, onDelete, onEdit, theme }) => {
  const primaryColor = theme?.palette.primary.main || '#60a5fa';
  const secondaryColor = theme?.palette.text.secondary || '#9ca3af';

  return (
    <Box 
      sx={{ 
        width: '100%',
        '&:hover': { backgroundColor: theme?.palette.action.hover || 'rgba(96, 165, 250, 0.08)' }
      }}
    >
      <Box 
        sx={{ 
          display: 'grid', 
          gridTemplateColumns: `repeat(3, 1fr)`, 
          gap: 0,
          width: '100%',
          p: 0,
          borderLeft: `3px solid ${primaryColor}`,
          backgroundColor: '#020d30',
          borderRadius: 1,
          '&:hover': {
            backgroundColor: '#04193a'
          }
        }}
      >
        {/* Edit button - Row 1, Column 1 */}
        <Box sx={{ gridColumn: '1 / 2', textAlign: 'left'}}>
          <IconButton 
            size="small" 
            onClick={(e) => { e.stopPropagation(); onEdit(); }}
            sx={{ color: primaryColor, '&:hover': { backgroundColor: theme?.palette.action.hover || 'rgba(96, 165, 250, 0.08)' } }}
          >
            <EventIcon fontSize="small" />
          </IconButton>
        </Box>

        {/* Title - Row 1, Column 2 */}
        <Box sx={{ gridColumn: '2 / 3', fontWeight: 600, color: primaryColor, cursor: 'default' }}>
          {event.title}
        </Box>

        {/* Delete button - Row 1, Column 3 */}
        <Box sx={{ gridColumn: '3 / -1', textAlign: 'right'}}>
          <IconButton 
            size="small" 
            onClick={(e) => { e.stopPropagation(); onDelete(event.id); }}
            sx={{ color: primaryColor, '&:hover': { backgroundColor: theme?.palette.action.hover || 'rgba(96, 165, 250, 0.08)' } }}
          >
            <DeleteIcon fontSize="small" />
          </IconButton>
        </Box>

        {/* Category - Spans all 3 columns */}
        <Box 
          sx={{ 
            gridColumn: 'span 3',
            textAlign: 'center',
            py: 0.75,
            fontSize: '0.6rem',
            fontWeight: 600,
            backgroundColor: `${primaryColor}15`,
            color: primaryColor,
            px: 1,
            borderRadius: 1,
            cursor: 'default'
          }}
        >
          {event.category}
        </Box>

        {/* Description - Spans all 3 columns */}
        <Box sx={{ gridColumn: 'span 3', cursor: 'default' }}>
          <Typography variant="caption" color={secondaryColor}>{event.description}</Typography>
        </Box>

        {/* Location - Spans all 3 columns */}
        <Box sx={{ gridColumn: 'span 3', textAlign: 'center', fontSize: '0.6rem', color: secondaryColor, cursor: 'default' }}>
          {event.location || 'TBD'}
        </Box>

        {/* Date - Spans all 3 columns */}
        <Box sx={{ gridColumn: 'span 3', textAlign: 'center', fontSize: '0.6rem', color: secondaryColor, cursor: 'default' }}>
          {new Date(event.date).toLocaleDateString()}
        </Box>
      </Box>
    </Box>
  );
};

export default EventCard;
