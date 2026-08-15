import './App.css'
import { Box, Container, Typography, Button } from '@mui/material'
import Calendar from './components/Calendar'
import EventDialog from './components/EventDialog'
import LoginPage from './components/LoginPage'
import ProtectedRoute from './components/ProtectedRoute'
import { useState, useEffect } from 'react'
import type { User, Event, PartialEvent } from './types'
import { useTheme } from '@mui/material/styles'
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom'
import { authService, authenticatedRequest } from './services/AuthService'

const API_BASE_URL = '/api/events'

function AppContent() {
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
            // Use authenticated request to fetch events for the current user
            const data = await authenticatedRequest<Event[]>('/api/events');
            setEvents(data);
        } catch (err) {
            console.error('Failed to load events from API:', err);
            // Fallback to mock data if not authenticated or API unavailable
            setEvents([
                { id: '1', title: 'Tech Conference 2026', description: 'A huge conference for developers to share knowledge.', date: '2026-08-15T10:00:00', location: 'San Francisco, CA', category: 'Tech' },
                { id: '2', title: 'Music Festival', description: 'Enjoy live music from various artists.', date: '2026-08-20T14:00:00', location: 'Austin, TX', category: 'Entertainment' },
                { id: '3', title: 'Art Gallery Opening', description: 'New exhibition by local artists.', date: '2026-08-25T18:00:00', location: 'New York, NY', category: 'Art' }
            ]);
        } finally {
            setIsLoading(false)
        }
    };

    const deleteEvent = async (id: string) => {
        try {
            await authenticatedRequest<void>(`/api/events/${id}`, { method: 'DELETE' });
            await fetchEvents();
        } catch (err) {
            console.error('Failed to delete event:', err);
            await fetchEvents();
        }
    };

    const handleOpenDialog = (event?: Event) => {
        setIsDialogOpen(true);
        setInitialEvent(event);
    };

    const handleCloseDialog = () => {
        setIsDialogOpen(false);
        setInitialEvent(undefined);
    };

    const handleSaveEvent = async (event: PartialEvent) => {
        try {
            let createdOrUpdatedEvent: Event;
            
            if (!event.id || event.id === '') {
                // New event - POST (no ID in body, server generates it)
                createdOrUpdatedEvent = await authenticatedRequest<Event>(API_BASE_URL, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(event)
                });
                setEvents(prev => [...prev, createdOrUpdatedEvent]);
            } else {
                // Existing event - PUT
                const response = await authenticatedRequest<Event>(`${API_BASE_URL}/${event.id}`, {
                    method: 'PUT',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(event)
                });
                setEvents(prev => prev.map(e => e.id === event.id ? response : e));
            }
            
            handleCloseDialog();
        } catch (err) {
            console.error('Failed to save event:', err);
            await fetchEvents();
        }
    };

    const handleLogout = async () => {
        try {
            await authenticatedRequest<void>('/api/auth/logout', { method: 'DELETE' });
        } catch (err) {
            console.error('Failed to logout from API:', err);
        } finally {
            authService.logout();
            window.location.href = '/login';
        }
    };

    return (
        <Box sx={{ minHeight: '100vh', backgroundColor: theme.palette.background.default }}>
            <Container maxWidth="lg" sx={{ py: 3 }}>
                {/* User info header */}
                <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2, px: 1 }}>
                    <Typography variant="h5">Event Calendar</Typography>
                    {authService.isAuthenticated() && (
                        <Box sx={{ display: 'flex', gap: 2, alignItems: 'center' }}>
                            {authService.getCurrentUser()?.picture && (
                                <img 
                                    src={authService.getCurrentUser()!.picture!} 
                                    alt="User" 
                                    style={{ width: 40, height: 40, borderRadius: '50%' }}
                                />
                            )}
                            <Typography variant="body2">{authService.getCurrentUser()?.displayName || 'Guest'}</Typography>
                            <Button onClick={handleLogout} color="error">Logout</Button>
                        </Box>
                    )}
                </Box>

                {/* Calendar */}
                <Calendar 
                    events={events} 
                    onOpenDialog={handleOpenDialog}
                    onDeleteEvent={deleteEvent}
                    theme={theme}
                />
                
                {isLoading && <div>Loading...</div>}
                
                {/* Event Dialog */}
                <EventDialog
                    open={isDialogOpen}
                    onClose={handleCloseDialog}
                    onSave={handleSaveEvent}
                    initialEvent={initialEvent}
                    theme={theme}
                />
            </Container>
        </Box>
    );
}

export default function App() {
    const [user, setUser] = useState<User | null>(null);

    useEffect(() => {
        // Check if user is already authenticated on page load
        const storedUser = authService.getCurrentUser();
        if (storedUser) {
            setUser(storedUser);
        }
    }, []);

    return (
        <Router>
            <Routes>
                {/* Public login route */}
                <Route path="/login" element={
                    user ? <Navigate to="/" replace /> : <LoginPage onLoginSuccess={setUser} />
                } />
                
                {/* Protected main app route */}
                <Route 
                    path="/" 
                    element={
                        <ProtectedRoute>
                            <AppContent />
                        </ProtectedRoute>
                    } 
                />
            </Routes>
        </Router>
    );
}