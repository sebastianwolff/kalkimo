import { apiClient, getErrorMessage } from './client';
import type { User, AuthTokens } from '@/stores/authStore';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  name: string;
}

export interface AuthResponse {
  user: User;
  accessToken: string;
  refreshToken: string;
  expiresAt: string; // ISO timestamp from backend
}

export interface RefreshRequest {
  refreshToken: string;
}

export interface RefreshResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string; // ISO timestamp from backend
}

// Auth API functions
export const authApi = {
  async login(request: LoginRequest): Promise<{ user: User; tokens: AuthTokens }> {
    try {
      const response = await apiClient.post<AuthResponse>('/auth/login', request);
      const { user, accessToken, refreshToken, expiresAt } = response.data;

      return {
        user,
        tokens: {
          accessToken,
          refreshToken,
          expiresAt: new Date(expiresAt).getTime()
        }
      };
    } catch (error) {
      throw new Error(getErrorMessage(error));
    }
  },

  async register(request: RegisterRequest): Promise<{ user: User; tokens: AuthTokens }> {
    try {
      const response = await apiClient.post<AuthResponse>('/auth/register', request);
      const { user, accessToken, refreshToken, expiresAt } = response.data;

      return {
        user,
        tokens: {
          accessToken,
          refreshToken,
          expiresAt: new Date(expiresAt).getTime()
        }
      };
    } catch (error) {
      throw new Error(getErrorMessage(error));
    }
  },

  async refresh(refreshToken: string): Promise<AuthTokens> {
    try {
      const response = await apiClient.post<RefreshResponse>('/auth/refresh', { refreshToken });
      const { accessToken, refreshToken: newRefreshToken, expiresAt } = response.data;

      return {
        accessToken,
        refreshToken: newRefreshToken,
        expiresAt: new Date(expiresAt).getTime()
      };
    } catch (error) {
      throw new Error(getErrorMessage(error));
    }
  },

  async logout(): Promise<void> {
    try {
      await apiClient.post('/auth/logout');
    } catch {
      // Ignore logout errors - just clear local state
    }
  }
};
