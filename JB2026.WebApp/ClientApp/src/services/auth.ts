import { apiClient } from './api'
import type {
  TokenResponse,
  UserProfile,
  TwoFactorSetupResponse,
  TwoFactorConfirmResponse,
  TwoFactorVerifyRequest,
  TwoFactorDisableRequest,
  TwoFactorStatusResponse,
} from '@/types/api'

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

export async function verifyTwoFactor(twoFactorToken: string, code: string): Promise<TokenResponse> {
  const response = await apiClient.post<TokenResponse>('/api/v2/auth/2fa/verify', {
    twoFactorToken,
    code,
  })
  return response.data
}

export async function setupTwoFactor(): Promise<TwoFactorSetupResponse> {
  const response = await apiClient.post<TwoFactorSetupResponse>('/api/v2/auth/2fa/setup')
  return response.data
}

export async function confirmTwoFactor(code: string): Promise<TwoFactorConfirmResponse> {
  const response = await apiClient.post<TwoFactorConfirmResponse>('/api/v2/auth/2fa/confirm', {
    code,
  })
  return response.data
}

export async function disableTwoFactor(password: string, code: string): Promise<void> {
  await apiClient.post('/api/v2/auth/2fa/disable', {
    password,
    code,
  })
}

export async function getTwoFactorStatus(): Promise<TwoFactorStatusResponse> {
  const response = await apiClient.get<TwoFactorStatusResponse>('/api/v2/auth/2fa/status')
  return response.data
}