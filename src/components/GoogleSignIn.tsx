import React, { useEffect, useState } from 'react';
import AuthService from '@/services/AuthService';

/**
 * Google Sign‑In component
 * - Loads the Google API script asynchronously.
 * - Renders a button that initiates Google OAuth flow.
 * - Handles the response (ID token) and stores the JWT in localStorage.
 * - Calls the backend `/api/auth/google/external-login` endpoint.
 */
const GoogleSignIn: React.FC = () => {
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Load Google API script dynamically
  useEffect(() => {
    const scriptId = 'google-api-script';
    if (!document.getElementById(scriptId)) {
      const script = document.createElement('script');
      script.id = scriptId;
      script.src = 'https://accounts.google.com/gsi/client';
      script.async = true;
      script.defer = true;
      document.head.appendChild(script);
    }

    // Once the script loads, initialize the Google Sign‑In client
    window.google?.accounts?.client?.initialized?.(() => {
      window.google?.accounts?.client?.renderButton(
        document.getElementById('gsi-button')!,
        {
          theme: 'outline',
          size: 'large',
          callback: (response: any) => {
            // response.credential contains the ID token
            const idToken = response.credential;
            // Store the token for subsequent API calls
            localStorage.setItem('authToken', idToken);
            // Optionally call the backend to create a session
            fetch('/api/auth/google/external-login', {
              method: 'POST',
              headers: {
                'Content-Type': 'application/json',
              },
              body: JSON.stringify({
                // The backend expects GoogleId, Email, Name – we extract them from the token client
                // The token client does not directly expose email/name, so we request them via
                // the Google Identity Services endpoint.
                // For simplicity, we forward the ID token; the backend will validate it.
                idToken,
              }),
            })
              .then((res) => {
                if (!res.ok) throw new Error('Login response was not ok');
                return res.json();
              })
              .then((data) => {
                // Store JWT returned by the API
                localStorage.setItem('jwt', data.token);
              })
              .catch((err) => setError(err.message));
          },
        }
      );
    });
  }, []);

  if (loading) return <div>Loading Google Sign‑In...</div>;

  return (
    <div>
      {error && <div style={{ color: 'red' }}>{error}</div>}
      <div id="gsi-button" style={{ width: '100%' }}></div>
    </div>
  );
};

export default GoogleSignIn;