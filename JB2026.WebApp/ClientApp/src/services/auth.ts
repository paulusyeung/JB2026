import { apiClient } from './api'
import type { TokenResponse, UserProfile } from '@/types/api'

export async function signIn(username: string, password: string, keepMeSignedIn: boolean = false): Promise<TokenResponse> {
  const response = await apiClient.post<TokenResponse>('/api/v2/auth/token', {
    username,
    password,
    keepMeSignedIn,
  })

  return response.data
}

export async function refreshToken(refreshToken: string): Promise<TokenResponse> {
  const response = await apiClient.post<TokenResponse>('/api/v2/auth/refresh', {
    refreshToken,
  })

  return response.data
}

export async function revokeToken(refreshToken: string): Promise<void> {
  await apiClient.post('/api/v2/auth/revoke', {
    refreshToken,
  })
}

export async function getCurrentUser(): Promise<UserProfile> {
  const response = await apiClient.get<UserProfile>('/api/v2/user-profiles/me')
  return response.data
}