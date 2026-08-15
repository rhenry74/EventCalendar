import { Box, Button, TextField, Typography } from '@mui/material';
import { useState } from 'react';
import { authService } from '../services/AuthService';
import type { User } from '../types';

interface LoginPageProps {
    onLoginSuccess: (user: User) => void;
}

export default function LoginPage({ onLoginSuccess }: LoginPageProps) {
    const [clientId, setClientId] = useState('');
    const [redirectUri, setRedirectUri] = useState('http://localhost:5173/callback');
    const [isLoading, setIsLoading] = useState(false);

    const handleLogin = async () => {
        if (!clientId) {
            alert('Please enter your Google Client ID');
            return;
        }

        setIsLoading(true);
        
        try {
            // Generate a unique state parameter for CSRF protection
            const generatedState = crypto.randomUUID();
            
            const response = await fetch('/api/auth/login', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    clientId,
                    redirectUri,
                    state: generatedState
                })
            });

            if (!response.ok) {
                throw new Error('Failed to initiate login');
            }

            const data = await response.json();
            
            // Store the state for later callback verification
            sessionStorage.setItem('oauth_state', generatedState);
            
            window.location.href = data.RedirectUrl;
        } catch (error) {
            console.error('Login error:', error);
            alert('Failed to initiate login. Please check your Client ID and try again.');
        } finally {
            setIsLoading(false);
        }
    };

    // Handle OAuth callback - this is called automatically by the browser after OAuth redirect
    // @ts-ignore - This function is intentionally not used directly; it's called via useEffect when URL changes
    const handleCallback = async () => {
        // Check if we have a token in the URL (from OAuth callback)
        const urlParams = new URLSearchParams(window.location.search);
        const token = urlParams.get('token');

        if (!token) {
            alert('No authentication token found. Please log in again.');
            window.location.href = '/';
            return;
        }

        try {
            // Fetch user info from API
            const response = await fetch('/api/auth/me', {
                method: 'GET',
                headers: {
                    'Authorization': `Bearer ${token}`,
                    'Content-Type': 'application/json'
                }
            });

            if (!response.ok) {
                throw new Error('Failed to get user info');
            }

            const user: User = await response.json();
            
            // Store authentication state
            await authService.setUserAndToken(user, token);
            
            onLoginSuccess(user);
        } catch (error) {
            console.error('Callback error:', error);
            alert('Failed to complete login. Please try again.');
            window.location.href = '/';
        }
    };

    return (
        <Box sx={{ 
            minHeight: '100vh', 
            display: 'flex', 
            flexDirection: 'column', 
            alignItems: 'center', 
            justifyContent: 'center',
            backgroundColor: '#f5f5f5'
        }}>
            <Typography variant="h4" component="h1" gutterBottom sx={{ mb: 2 }}>
                Event Calendar - Login
            </Typography>
            
            <Box sx={{ width: '100%', maxWidth: 400, p: 3, bgcolor: 'white', borderRadius: 2, boxShadow: 3 }}>
                <Typography variant="body1" gutterBottom sx={{ mb: 3 }}>
                    Sign in with Google to access your calendar.
                </Typography>

                <TextField
                    fullWidth
                    label="Google Client ID"
                    value={clientId}
                    onChange={(e) => setClientId(e.target.value)}
                    placeholder="Enter your Google OAuth Client ID"
                    variant="outlined"
                    margin="normal"
                    required
                />

                <TextField
                    fullWidth
                    label="Redirect URI"
                    value={redirectUri}
                    onChange={(e) => setRedirectUri(e.target.value)}
                    placeholder="http://localhost:5173/callback"
                    variant="outlined"
                    margin="normal"
                    helperText="This should match your OAuth redirect URI"
                />

                <Button
                    fullWidth
                    variant="contained"
                    color="primary"
                    onClick={handleLogin}
                    disabled={isLoading || !clientId}
                    sx={{ mt: 3, mb: 2 }}
                >
                    {isLoading ? 'Logging in...' : 'Sign In with Google'}
                </Button>

                <Typography variant="body2" color="text.secondary" align="center">
                    Or continue as guest (limited functionality)
                </Typography>
            </Box>

            <Typography variant="caption" color="text.secondary" sx={{ mt: 3 }}>
                OAuth login requires a valid Google Client ID.
                Contact your administrator to obtain one.
            </Typography>
        </Box>
    );
}