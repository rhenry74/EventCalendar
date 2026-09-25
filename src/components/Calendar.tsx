import React, { useState, useMemo } from 'react';
import type { CalendarDate, Category, Event } from '../types';
import EventCard from './EventCard';
import { Box, Button, Typography } from '@mui/material';
import { ChevronLeft, ChevronRight } from '@mui/icons-material';
import type { Theme } from '@mui/material/styles';
import { dateOnlyPart } from '../dateUtils';

const calendarDateKey = (date: Date) => new Date(Date.UTC(
  date.getFullYear(), date.getMonth(), date.getDate()
)).toISOString().split('T')[0];

const eventDateKey = (value: string) => {
  // Date-only values, including UTC-midnight values from MCP clients, represent
  // a local calendar date rather than an instant in time.
  const dateOnly = dateOnlyPart(value);
  if (dateOnly) return dateOnly;
  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) return null;
  // Timed values are stored as instants (usually ISO UTC), but calendar
  // placement should use the user's local date. Using toISOString() here can
  // move a late-evening event onto the following UTC day.
  return calendarDateKey(parsed);
};

const eventOccursOnDay = (event: Event, date: Date) => {
  const dayKey = calendarDateKey(date);
  const startKey = eventDateKey(event.date);
  const endKey = event.endDate ? eventDateKey(event.endDate) : startKey;
  return !!startKey && !!endKey && dayKey >= startKey && dayKey <= endKey;
};

type EventDayStatus = 'single' | 'start' | 'continued' | 'last';

const eventDayStatus = (event: Event, date: Date): EventDayStatus => {
  const startKey = eventDateKey(event.date);
  const endKey = event.endDate ? eventDateKey(event.endDate) : startKey;
  const dayKey = calendarDateKey(date);
  if (!startKey || !endKey || startKey === endKey) return 'single';
  if (dayKey === startKey) return 'start';
  if (dayKey === endKey) return 'last';
  return 'continued';
};

interface CalendarProps {
  events: Event[];
  categories: Category[];
  onOpenDialog: (event?: Event) => void;
  onDeleteEvent: (id: string, callback?: () => void) => Promise<void>;
  theme: Theme;
}

const Calendar: React.FC<CalendarProps> = ({ events, categories, onOpenDialog, onDeleteEvent, theme }) => {
  const [currentMonth, setCurrentMonth] = useState(() => {
    const today = new Date();
    return new Date(today.getFullYear(), today.getMonth(), 1);
  });
  
  const year = currentMonth.getFullYear();
  const month = currentMonth.getMonth();
  const todayKey = calendarDateKey(new Date());
  
  // Days in current month
  const daysInMonth = new Date(year, month + 1, 0).getDate();
  // Last day of previous month
  const lastDayPrevMonth = new Date(year, month, 0).getDate();
  // Day of week for the 1st of current month (Sun=0, Mon=1, ..., Sat=6)
  const firstDayOfWeek = new Date(year, month, 1).getDay();

  // Generate days from previous month to fill empty cells on left
  const prevMonthDays: number[] = [];
  for (let i = 0; i < firstDayOfWeek; i++) {
    prevMonthDays.push(lastDayPrevMonth - i);
  }
  prevMonthDays.reverse(); // Show days in correct order

  // Calculate how many cells are needed after current month to complete grid rows
  // Total cells in full grid = firstDayOfWeek (prev month padding) + daysInMonth (current) + nextMonthDays
  // We want the total to be a multiple of 7 (complete rows)
  const cellsAfterCurrentMonth = firstDayOfWeek + daysInMonth;
  const cellsNeededToCompleteGrid = Math.ceil(cellsAfterCurrentMonth / 7) * 7;
  const remainingCells = cellsNeededToCompleteGrid - cellsAfterCurrentMonth;
  
  // Generate days from next month to fill empty cells on right
  const nextMonthDays: number[] = [];
  for (let i = 1; i <= remainingCells && i < 32; i++) {
    nextMonthDays.push(i);
  }

  // Helper function to get events for any given day
  const getEventsForDay = (dayNum: number, isPrevMonth = false, isNextMonth = false): Event[] => {
    if (isPrevMonth) {
      const date = new Date(year, month - 1, dayNum);
      return events.filter(e => eventOccursOnDay(e, date));
    }
    
    if (isNextMonth) {
      const date = new Date(year, month + 1, dayNum);
      return events.filter(e => eventOccursOnDay(e, date));
    }

    // Current month days
    const date = new Date(year, month, dayNum);
    return events.filter(e => eventOccursOnDay(e, date));
  };

  const calendarDays = useMemo(() => {
    const days: CalendarDate[] = [];
    
    for (let d = 1; d <= daysInMonth; d++) {
      const date = new Date(year, month, d);
      
      // Filter events for the current day
      const dayEvents = events.filter(e => eventOccursOnDay(e, date));
      
      days.push({
        day: d,
        date,
        events: dayEvents
      });
    }
    
    return days;
  }, [currentMonth, events]);

  const handlePrevMonth = () => setCurrentMonth(new Date(currentMonth.getFullYear(), currentMonth.getMonth() - 1));
  const handleNextMonth = () => setCurrentMonth(new Date(currentMonth.getFullYear(), currentMonth.getMonth() + 1));

  const monthName = currentMonth.toLocaleString('default', { month: 'long' });

  return (
    <Box 
      sx={{ 
        display: 'flex', 
        flexDirection: 'column', 
        gap: 2,
        p: { xs: 1, sm: 2 },
        backgroundColor: theme.palette.background.default,
        borderRadius: 2,
        border: `1px solid ${theme.palette.divider}`,
        minHeight: '600px'
      }}
    >
      <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', flexWrap: 'wrap', gap: 1, mb: 2 }}>
        <Button 
          onClick={handlePrevMonth}
          startIcon={<ChevronLeft />}
          sx={{ color: theme.palette.text.primary, '&:hover': { backgroundColor: theme.palette.action.hover, color: theme.palette.primary.main } }}
        >Prev</Button>
        <Typography variant="h4" sx={{ fontWeight: 600, color: theme.palette.text.primary, textAlign: 'center', flexGrow: 1, order: { xs: -1, sm: 0 }, width: { xs: '100%', sm: 'auto' }, fontSize: { xs: '1.5rem', sm: '2.125rem' } }}>{monthName} {year}</Typography>
        <Button 
          onClick={handleNextMonth}
          endIcon={<ChevronRight />}
          sx={{ color: theme.palette.text.primary, '&:hover': { backgroundColor: theme.palette.action.hover, color: theme.palette.primary.main } }}
        >Next</Button>
        <Button 
          variant="contained" 
          onClick={() => onOpenDialog()}
          sx={{ 
            backgroundColor: theme.palette.primary.main, 
            color: '#fff', 
            '&:hover': { backgroundColor: theme.palette.primary.main },
            fontWeight: 600
          }}
        >Add Event</Button>
      </Box>
      
      <Box 
        sx={{ 
          width: '100%',
          overflowX: 'auto',
          WebkitOverflowScrolling: 'touch',
          pb: 1,
        }}
      >
      <Box
        sx={{
          display: 'grid', 
          gridTemplateColumns: 'repeat(7, minmax(96px, 1fr))',
          minWidth: { xs: 672, sm: 0 },
          gap: theme.spacing(1),
          flex: 1
        }}
      >
        {/* Day of week headers */}
        {['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'].map(day => (
          <Box 
            key={day} 
            sx={{ 
              display: 'flex', 
              alignItems: 'center', 
              justifyContent: 'center', 
              fontWeight: 600,
              color: theme.palette.text.primary,
              borderBottom: `1px solid ${theme.palette.divider}`,
              pb: 1
            }}
          >{day}</Box>
        ))}
        
        {/* Days from previous month */}
        {prevMonthDays.map((day) => (
          <Box 
            key={`prev-${day}`} 
            sx={{ 
              display: 'flex', 
              flexDirection: 'column', 
              gap: 1,
              opacity: 0.5,
              color: theme.palette.text.secondary
            }}
          >
            <span>{day}</span>
            <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
              {getEventsForDay(day, true).map(event => (
                <EventCard 
                  key={event.id} 
                  event={event} 
                  categories={categories}
                  dayStatus={eventDayStatus(event, new Date(year, month - 1, day))}
                  onDelete={async () => {
                    try {
                      await onDeleteEvent(event.id);
                    } catch (err) {
                      console.error('Failed to delete event:', err);
                    }
                  }} 
                  onEdit={() => onOpenDialog(event)} 
                />
              ))}
            </Box>
          </Box>
        ))}
        
        {/* Current month days */}
        {calendarDays.map((dayData) => (
          <Box 
            key={dayData.date.toISOString()} 
            sx={{ 
              display: 'flex', 
              flexDirection: 'column', 
              gap: 1,
              color: theme.palette.text.primary
            }}
          >
            <Box
              component="span"
              sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', width: '100%', height: 28 }}
            >
              <Box
                component="span"
                sx={calendarDateKey(dayData.date) === todayKey ? {
                  display: 'inline-flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  width: 28,
                  height: 28,
                  borderRadius: '50%',
                  backgroundColor: theme.palette.primary.main,
                  color: theme.palette.primary.contrastText,
                  fontWeight: 700
                } : { display: 'inline-flex', alignItems: 'center', height: 28 }}
              >{dayData.day}</Box>
            </Box>
            <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
              {dayData.events.map(event => (
                <EventCard 
                  key={event.id} 
                  event={event} 
                  categories={categories}
                  dayStatus={eventDayStatus(event, dayData.date)}
                  onDelete={async () => {
                    try {
                      await onDeleteEvent(event.id);
                    } catch (err) {
                      console.error('Failed to delete event:', err);
                    }
                  }} 
                  onEdit={() => onOpenDialog(event)} 
                />
              ))}
            </Box>
          </Box>
        ))}
        
        {/* Days from next month */}
        {nextMonthDays.map((day) => (
          <Box 
            key={`next-${day}`} 
            sx={{ 
              display: 'flex', 
              flexDirection: 'column', 
              gap: 1,
              opacity: 0.5,
              color: theme.palette.text.secondary
            }}
          >
            <span>{day}</span>
            <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
              {getEventsForDay(day, false, true).map(event => (
                <EventCard 
                  key={event.id} 
                  event={event} 
                  categories={categories}
                  dayStatus={eventDayStatus(event, new Date(year, month + 1, day))}
                  onDelete={async () => {
                    try {
                      await onDeleteEvent(event.id);
                    } catch (err) {
                      console.error('Failed to delete event:', err);
                    }
                  }} 
                  onEdit={() => onOpenDialog(event)} 
                />
              ))}
            </Box>
          </Box>
        ))}
      </Box>
      </Box>
    </Box>
  );
};

export default Calendar;
