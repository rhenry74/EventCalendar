import type { User } from '../types';

// SingletonAuthService class - manages authentication state
export class SingletonAuthService {
    private static instance: SingletonAuthService | null = null;
    protected currentUser: User | null = null;
    private token: string | null = null;

    // Private constructor for singleton pattern
    private constructor() {}

    // Get the singleton instance
    public static getInstance(): SingletonAuthService {
        if (!SingletonAuthService.instance) {
            SingletonAuthService.instance = new SingletonAuthService();
        }
        return SingletonAuthService.instance!;
    }

    // Set user and token from API response
    public async setUserAndToken(user: User, token: string): Promise<void> {
        this.currentUser = user;
        this.token = token;
        localStorage.setItem('jwt_token', token);
        localStorage.setItem('current_user', JSON.stringify(user));
    }

    // Clear authentication state (logout)
    public async logout(): Promise<void> {
        this.currentUser = null;
        this.token = null;
        localStorage.removeItem('jwt_token');
        localStorage.removeItem('current_user');
    }

    // Get current user from storage or session
    public getCurrentUser(): User | null {
        const storedUser = localStorage.getItem('current_user');
        if (storedUser) {
            try {
                const parsedUser: User = JSON.parse(storedUser);
                this.currentUser = parsedUser;
                return parsedUser;
            } catch (e) {
                console.error('Error parsing user from storage:', e);
                localStorage.removeItem('current_user');
            }
        }
        // Check if token exists in session
        const storedToken = sessionStorage.getItem('jwt_token');
        if (storedToken && !this.token) {
            this.token = storedToken;
        }
        
        return this.currentUser;
    }

    // Get current user - public access for external use
    public getUser(): User | null {
        return this.currentUser;
    }

    // Get current token
    public getToken(): string | null {
        const storedToken = localStorage.getItem('jwt_token') || sessionStorage.getItem('jwt_token');
        if (storedToken && !this.token) {
            this.token = storedToken;
        }
        return this.token;
    }

    // Check if user is authenticated
    public isAuthenticated(): boolean {
        const token = this.getToken();
        const user = this.getCurrentUser();
        return !!token || !!user;
    }

    // Clear all authentication data
    public clearAll(): void {
        localStorage.clear();
        sessionStorage.clear();
        this.currentUser = null;
        this.token = null;
    }
}

// Export singleton instance for easy access
export const authService = SingletonAuthService.getInstance();

// Helper function to check if route should be protected
export function isProtectedRoute(): boolean {
    return authService.isAuthenticated();
}

// Helper function to get current user (for API calls)
export async function getCurrentUserFromApi(): Promise<User | null> {
    const token = authService.getToken();
    if (!token) return null;

    try {
        const response = await fetch('/api/auth/me', {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            }
        });

        if (response.ok) {
            const user: User = await response.json();
            authService.setUserAndToken(user, token);
            return user;
        } else {
            // Token might be invalid, clear auth state
            authService.logout();
            return null;
        }
    } catch (error) {
        console.error('Error fetching current user:', error);
        authService.logout();
        return null;
    }
}

// Helper function to make authenticated API requests
export async function authenticatedRequest<T>(endpoint: string, options: RequestInit = {}): Promise<T> {
    const token = authService.getToken();
    
    if (!token) {
        throw new Error('Not authenticated');
    }

    const headers: HeadersInit = {
        'Authorization': `Bearer ${token}`,
        ...options.headers
    };

    const response = await fetch(endpoint, {
        ...options,
        headers
    });

    if (!response.ok) {
        throw new Error(`API error: ${response.status}`);
    }

    return response.json();
}