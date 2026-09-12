import './App.css'
import { Alert, Box, Button, Chip, Snackbar, Typography } from '@mui/material'
import Calendar from './components/Calendar'
import EventDialog from './components/EventDialog'
import { useState, useEffect, useRef } from 'react'
import type { ChangeEvent } from 'react'
import type { Category, Event, PartialEvent } from './types'
import { useTheme } from '@mui/material/styles'

const API_ROOT = import.meta.env.VITE_API_BASE_URL ?? '/api'
const API_BASE_URL = `${API_ROOT}/events`

interface SignedInUser {
  id: string
  name?: string
  email?: string
}

function App() {
  const theme = useTheme();
  const [events, setEvents] = useState<Event[]>([])
  const [categories, setCategories] = useState<Category[]>([])
  const [isDialogOpen, setIsDialogOpen] = useState(false)
  const [initialEvent, setInitialEvent] = useState<Event | undefined>(undefined)
  const [isLoading, setIsLoading] = useState(true)
  const [user, setUser] = useState<SignedInUser | null>(null)
  const [isMcpTokenLoading, setIsMcpTokenLoading] = useState(false)
  const [mcpToast, setMcpToast] = useState<{ severity: 'success' | 'error'; message: string } | null>(null)
  const uploadInputRef = useRef<HTMLInputElement | null>(null)

  useEffect(() => {
    const load = async () => {
      try {
        const response = await fetch(`${API_ROOT}/auth/me`, { credentials: 'include' })
        if (!response.ok) return
        const signedInUser = await response.json()
        setUser(signedInUser)
        await Promise.all([fetchEvents(), fetchCategories()])
      } catch (error) {
        console.error('Failed to check the current session.', error)
      } finally {
        setIsLoading(false)
      }
    }
    load()
  }, [])

  const fetchEvents = async () => {
    try {
      const response = await fetch(API_BASE_URL, { credentials: 'include' })
      if (!response.ok) {
        throw new Error('Failed to fetch events')
      }
      const data = await response.json()
      setEvents(data)
    } catch (err) {
      console.error('Failed to load events from API.', err)
    } finally {
      setIsLoading(false)
    }
  }

  const deleteEvent = async (id: string) => {
    try {
      const response = await fetch(`${API_BASE_URL}/${id}`, { method: 'DELETE', credentials: 'include' })
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
          credentials: 'include',
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
          credentials: 'include',
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

  const logOut = async () => {
    await fetch(`${API_ROOT}/auth/logout`, { method: 'POST', credentials: 'include' })
    setEvents([])
    setUser(null)
  }

  const fetchCategories = async () => {
    try {
      const response = await fetch('/categories.json', { cache: 'no-store' })
      if (!response.ok) throw new Error('Failed to fetch categories')
      const data = await response.json() as Category[]
      setCategories(data)
    } catch (err) {
      console.error('Failed to load categories.', err)
    }
  }

  const copyMcpToken = async () => {
    setIsMcpTokenLoading(true)
    try {
      const response = await fetch(`${API_ROOT}/mcp/token`, {
        method: 'POST',
        credentials: 'include',
      })
      if (!response.ok) throw new Error('Unable to create an MCP token.')

      const data = await response.json() as { token?: string }
      if (!data.token) throw new Error('The API did not return an MCP token.')
      await navigator.clipboard.writeText(data.token)
      setMcpToast({ severity: 'success', message: 'Token copied' })
    } catch (error) {
      console.error('Failed to create or copy the MCP token.', error)
      setMcpToast({ severity: 'error', message: 'Could not copy MCP token' })
    } finally {
      setIsMcpTokenLoading(false)
    }
  }

  const downloadBackup = async () => {
    try {
      const response = await fetch(`${API_ROOT}/events/backup`, { credentials: 'include' })
      if (!response.ok) throw new Error('Unable to download the event backup.')
      const blob = await response.blob()
      const url = URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = url
      link.download = 'events.json'
      link.click()
      URL.revokeObjectURL(url)
    } catch (error) {
      console.error('Failed to download the event backup.', error)
      setMcpToast({ severity: 'error', message: 'Could not download events.json' })
    }
  }

  const uploadBackup = async (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0]
    event.target.value = ''
    if (!file) return

    if (!window.confirm('Upload this events.json and replace all calendar data?')) return

    try {
      const content = JSON.parse(await file.text())
      if (!Array.isArray(content)) throw new Error('The selected file must contain a JSON array.')
      const response = await fetch(`${API_ROOT}/events/backup`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify(content),
      })
      if (!response.ok) throw new Error('The API rejected the event backup.')
      await fetchEvents()
      setMcpToast({ severity: 'success', message: 'events.json uploaded' })
    } catch (error) {
      console.error('Failed to upload the event backup.', error)
      setMcpToast({ severity: 'error', message: 'Could not upload events.json' })
    }
  }

  if (isLoading) return <Box sx={{ p: 4 }}>Loading…</Box>

  if (!user) {
    return (
      <Box sx={{ minHeight: '100vh', display: 'grid', placeItems: 'center', backgroundColor: theme.palette.background.default }}>
        <Box sx={{ textAlign: 'center' }}>
          <Typography variant="h3" gutterBottom>EventCalendar</Typography>
          <Typography sx={{ mb: 3 }}>Sign in to create private events or share public ones.</Typography>
          <Button variant="contained" href={`${API_ROOT}/auth/login`}>Continue with Google</Button>
        </Box>
      </Box>
    )
  }

  return (
    <Box sx={{ minHeight: '100vh', backgroundColor: theme.palette.background.default }}>
      <Box sx={{ display: 'flex', justifyContent: 'flex-end', alignItems: 'center', flexWrap: 'wrap', gap: 1, px: { xs: 1, sm: 2 }, pt: 2 }}>
        <Typography sx={{ maxWidth: '100%', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{user.name ?? user.email}</Typography>
        <Chip
          label={isMcpTokenLoading ? 'MCP…' : 'MCP'}
          size="small"
          color="primary"
          variant="outlined"
          clickable={!isMcpTokenLoading}
          disabled={isMcpTokenLoading}
          onClick={copyMcpToken}
          title="Copy a new MCP token for this account"
        />
        {user.email === 'rhenry74@gmail.com' && (
          <>
            <Chip label="BACKUP" size="small" color="secondary" variant="outlined" clickable onClick={downloadBackup} />
            <Chip label="UPLOAD" size="small" color="secondary" variant="outlined" clickable onClick={() => uploadInputRef.current?.click()} />
            <input
              ref={uploadInputRef}
              type="file"
              accept=".json,application/json"
              hidden
              onChange={uploadBackup}
            />
          </>
        )}
        <Button onClick={logOut}>Sign out</Button>
      </Box>
      <Calendar 
        events={events} 
        categories={categories}
        onOpenDialog={handleOpenDialog}
        onDeleteEvent={deleteEvent}
        theme={theme}
      />
      <EventDialog
        open={isDialogOpen}
        onClose={handleCloseDialog}
        onSave={handleSaveEvent}
        initialEvent={initialEvent}
        categories={categories}
        theme={theme}
      />
      <Snackbar
        open={!!mcpToast}
        autoHideDuration={2500}
        onClose={() => setMcpToast(null)}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
      >
        <Alert onClose={() => setMcpToast(null)} severity={mcpToast?.severity ?? 'success'} variant="filled">
          {mcpToast?.message}
        </Alert>
      </Snackbar>
    </Box>
  )
}

export default App
