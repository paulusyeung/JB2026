import { apiClient } from './api'

export interface UserPreferenceResponse {
  metadata: string | null
}

export interface SaveUserPreferenceRequest {
  metadata: string
}

export async function getUserPreference(objectType: number, objectId: string): Promise<UserPreferenceResponse> {
  const response = await apiClient.get<UserPreferenceResponse>(`/api/v2/user-preferences/${objectType}/${objectId}`)
  return response.data
}

export async function saveUserPreference(
  objectType: number,
  objectId: string,
  payload: SaveUserPreferenceRequest,
): Promise<UserPreferenceResponse> {
  const response = await apiClient.put<UserPreferenceResponse>(`/api/v2/user-preferences/${objectType}/${objectId}`, payload)
  return response.data
}
