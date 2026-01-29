import { defineStore } from 'pinia';
import { ref, computed } from 'vue';

export interface User {
  id: string;
  email: string;
  name: string;
  roles: string[];
  entitlements?: string[];
}

export interface AuthTokens {
  accessToken: string;
  refreshToken: string;
  expiresAt: number;
}

const TOKEN_STORAGE_KEY = 'kalkimo_auth_tokens';
const USER_STORAGE_KEY = 'kalkimo_user';

export const useAuthStore = defineStore('auth', () => {
  // State
  const user = ref<User | null>(null);
  const tokens = ref<AuthTokens | null>(null);
  const isLoading = ref(false);
  const error = ref<string | null>(null);

  // Getters
  const isAuthenticated = computed(() => !!tokens.value && Date.now() < tokens.value.expiresAt);
  const isOwner = computed(() => user.value?.roles?.includes('Owner') ?? false);
  const isAdmin = computed(() => user.value?.roles?.includes('Admin') ?? false);
  const displayName = computed(() => user.value?.name ?? user.value?.email ?? '');

  // Initialize from storage
  function initFromStorage() {
    try {
      const storedTokens = localStorage.getItem(TOKEN_STORAGE_KEY);
      const storedUser = localStorage.getItem(USER_STORAGE_KEY);

      if (storedTokens) {
        const parsed = JSON.parse(storedTokens) as AuthTokens;
        if (Date.now() < parsed.expiresAt) {
          tokens.value = parsed;
        } else {
          localStorage.removeItem(TOKEN_STORAGE_KEY);
        }
      }

      if (storedUser) {
        user.value = JSON.parse(storedUser) as User;
      }
    } catch {
      // Clear corrupted storage
      localStorage.removeItem(TOKEN_STORAGE_KEY);
      localStorage.removeItem(USER_STORAGE_KEY);
    }
  }

  // Actions
  function setAuth(newUser: User, newTokens: AuthTokens) {
    user.value = newUser;
    tokens.value = newTokens;

    localStorage.setItem(TOKEN_STORAGE_KEY, JSON.stringify(newTokens));
    localStorage.setItem(USER_STORAGE_KEY, JSON.stringify(newUser));

    error.value = null;
  }

  function updateTokens(newTokens: AuthTokens) {
    tokens.value = newTokens;
    localStorage.setItem(TOKEN_STORAGE_KEY, JSON.stringify(newTokens));
  }

  function logout() {
    user.value = null;
    tokens.value = null;

    localStorage.removeItem(TOKEN_STORAGE_KEY);
    localStorage.removeItem(USER_STORAGE_KEY);
  }

  function setLoading(loading: boolean) {
    isLoading.value = loading;
  }

  function setError(err: string | null) {
    error.value = err;
  }

  // Check if token needs refresh (within 5 minutes of expiry)
  function needsRefresh(): boolean {
    if (!tokens.value) return false;
    const fiveMinutes = 5 * 60 * 1000;
    return Date.now() > (tokens.value.expiresAt - fiveMinutes);
  }

  function getAccessToken(): string | null {
    return tokens.value?.accessToken ?? null;
  }

  function getRefreshToken(): string | null {
    return tokens.value?.refreshToken ?? null;
  }

  return {
    // State
    user,
    tokens,
    isLoading,
    error,

    // Getters
    isAuthenticated,
    isOwner,
    isAdmin,
    displayName,

    // Actions
    initFromStorage,
    setAuth,
    updateTokens,
    logout,
    setLoading,
    setError,
    needsRefresh,
    getAccessToken,
    getRefreshToken
  };
});
