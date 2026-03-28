import { apiClient } from './api'
import type { TokenResponse, UserProfile } from '@/types/api'

export async function signIn(username: string, password: string): Promise<TokenResponse> {
  const response = await apiClient.post<TokenResponse>('/api/v2/auth/token', {
    username,
    password,
  })

  return response.data
}

export async function getCurrentUser(): Promise<UserProfile> {
  const response = await apiClient.get<UserProfile>('/api/v2/user-profiles/me')
  return response.data
}