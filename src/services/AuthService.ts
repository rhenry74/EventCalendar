/**
 * AuthService – centralizes token storage and provides the current
 * JWT for use in API requests.  The token is stored in localStorage
 * under the key "jwt" (the same key used by ApiClient).
 *
 * Typical usage:
 *   const token = await AuthService.getToken();
 *   // Pass `token` to your API calls or let ApiClient add it automatically.
 */
class AuthService {
  private static readonly TOKEN_KEY = 'jwt';

  /** Retrieve the current token */
  static getToken(): string | null {
    return localStorage.getItem(AuthService.TOKEN_KEY);
  }

  /** Store a new token */
  static setToken(token: string): void {
    localStorage.setItem(AuthService.TOKEN_KEY, token);
  }

  /** Remove the token (e.g., on logout) */
  static clearToken(): void {
    localStorage.removeItem(AuthService.TOKEN_KEY);
  }
}

export default AuthService;