import './App.css'
import { Box } from '@mui/material'
import Calendar from './components/Calendar'
import EventDialog from './components/EventDialog'
import { useState, useEffect } from 'react'
import type { Event, PartialEvent } from './types'
import { useTheme } from '@mui/material/styles'

const API_BASE_URL = '/api/events'

function App() {
  const theme = useTheme();
  const [events, setEvents] = useState<Event[]>([])
  const [isDialogOpen, setIsDialogOpen] = useState(false)
  const [initialEvent, setInitialEvent] = useState<Event | undefined>(undefined)
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    fetchEvents().catch(console.error)
  }, [])

  const fetchEvents = async () => {
    try {
      const response = await fetch(API_BASE_URL)
      if (!response.ok) {
        throw new Error('Failed to fetch events')
      }
      const data = await response.json()
      setEvents(data)
    } catch (err) {
      console.error('Failed to load events from API. Using mock data.')
      setEvents([
        { id: '1', title: 'Tech Conference 2026', description: 'A huge conference for developers to share knowledge.', date: '2026-08-15T10:00:00', location: 'San Francisco, CA', category: 'Tech' },
        { id: '2', title: 'Music Festival', description: 'Enjoy live music from various artists.', date: '2026-08-20T14:00:00', location: 'Austin, TX', category: 'Entertainment' },
        { id: '3', title: 'Art Gallery Opening', description: 'New exhibition by local artists.', date: '2026-08-25T18:00:00', location: 'New York, NY', category: 'Art' }
      ])
    } finally {
      setIsLoading(false)
    }
  }

  const deleteEvent = async (id: string) => {
    try {
      const response = await fetch(`${API_BASE_URL}/${id}`, { method: 'DELETE' })
      if (response.ok) {
        await fetchEvents()
      } else {
        console.error('Failed to delete event')
      }
    } catch (err) {
      console.error('Failed to delete event:', err)
      await fetchEvents()
    }
  }

  const handleOpenDialog = (event?: Event) => {
    setIsDialogOpen(true);
    setInitialEvent(event);
  }

  const handleCloseDialog = () => {
    setIsDialogOpen(false);
    setInitialEvent(undefined);
  }

  const handleSaveEvent = async (event: PartialEvent) => {
    try {
      let response: Response;
      
      if (!event.id || event.id === '') {
        // New event - POST (no ID in body, server generates it)
        response = await fetch(API_BASE_URL, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(event)
        })
        // Read the created event from the 201 response
        const createdEvent = await response.json()
        setEvents(prev => [...prev, createdEvent])
      } else {
        // Existing event - PUT
        response = await fetch(`${API_BASE_URL}/${event.id}`, {
          method: 'PUT',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(event)
        })
        if (response.ok) {
          // Read the updated event back
          const updatedEvent = await response.json()
          setEvents(prev => prev.map(e => e.id === event.id ? updatedEvent : e))
        } else if (response.status === 404) {
          console.error('Event not found for update')
          handleCloseDialog()
          return
        }
      }
      
      handleCloseDialog();
    } catch (err) {
      console.error('Failed to save event:', err)
      // Fallback: refetch all events on error
      await fetchEvents()
    }
  }

  return (
    <Box sx={{ minHeight: '100vh', backgroundColor: theme.palette.background.default }}>
      <Calendar 
        events={events} 
        onOpenDialog={handleOpenDialog}
        onDeleteEvent={deleteEvent}
        theme={theme}
      />
      {isLoading && <div>Loading...</div>}
      <EventDialog
        open={isDialogOpen}
        onClose={handleCloseDialog}
        onSave={handleSaveEvent}
        initialEvent={initialEvent}
        theme={theme}
      />
    </Box>
  )
}

export default App