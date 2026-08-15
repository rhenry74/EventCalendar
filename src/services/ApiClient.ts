/**
 * ApiClient – a tiny wrapper around `fetch` that automatically
 * adds the JWT token (stored in localStorage under the key "jwt")
 * to the Authorization header for every request.
 *
 * Usage:
 *   import ApiClient from '@/services/ApiClient';
 *   const response = await ApiClient.get('/api/events');
 *   const data = await response.json();
 */
class ApiClient {
  private static readonly TOKEN_KEY = 'jwt';

  private static async getAuthHeaders(): Promise<HeadersInit> {
    const token = localStorage.getItem(ApiClient.TOKEN_KEY);
    if (!token) return {};

    return {
      Authorization: `Bearer ${token}`,
    };
  }

  private static async prepareInput(input: RequestInfo | URL, init?: RequestInit): Promise<RequestInit> {
    const headers = new Headers(init?.headers);
    const authHeaders = await this.getAuthHeaders();
    Object.entries(authHeaders).forEach(([k, v]) => headers.set(k, v as any));
    return {
      ...init,
      headers,
    };
  }

  static async get<T>(url: string): Promise<T> {
    const response = await fetch(url, {
      method: 'GET',
      credentials: 'same-origin',
      ...this.prepareInput(url, { method: 'GET' }),
    });
    if (!response.ok) throw new Error(`HTTP ${response.status}`);
    return response.json() as Promise<T>;
  }

  static async post<T>(url: string, body: any): Promise<T> {
    const response = await fetch(url, {
      method: 'POST',
      credentials: 'same-origin',
      headers: {
        'Content-Type': 'application/json',
        ...(await this.getAuthHeaders()),
      },
      body: JSON.stringify(body),
    });
    if (!response.ok) throw new Error(`HTTP ${response.status}`);
    return response.json() as Promise<T>;
  }

  static async put<T>(url: string, body: any): Promise<T> {
    const response = await fetch(url, {
      method: 'PUT',
      credentials: 'same-origin',
      headers: {
        'Content-Type': 'application/json',
        ...(await this.getAuthHeaders()),
      },
      body: JSON.stringify(body),
    });
    if (!response.ok) throw new Error(`HTTP ${response.status}`);
    return response.json() as Promise<T>;
  }

  static async del(url: string): Promise<void> {
    const response = await fetch(url, {
      method: 'DELETE',
      credentials: 'same-origin',
      ...this.prepareInput(url, { method: 'DELETE' }),
    });
    if (!response.ok) throw new Error(`HTTP ${response.status}`);
  }
}

export default ApiClient;