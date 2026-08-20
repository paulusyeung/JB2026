import { apiClient } from './api'

/**
 * CRM (Twenty) integration settings from appsettings.json.
 * Sensitive values are masked by the server.
 */
export interface CrmSettings {
  configured: boolean
  baseUrl: string
  apiKey: string
  httpClientTimeoutSeconds: number
}

/**
 * DMS (Paperless-ngx) integration settings from appsettings.json.
 * Sensitive values are masked by the server.
 */
export interface DmsSettings {
  configured: boolean
  baseUrl: string
  apiToken: string
  defaultUser: string
  httpClientTimeoutSeconds: number
}

/**
 * Email (Mailcow) integration settings from appsettings.json.
 * Sensitive values are masked by the server.
 */
export interface EmailSettings {
  configured: boolean
  baseUrl: string
  fallbackAccountEmail: string
  fallbackAccountPassword: string
  imapPort: number
  useSsl: boolean
  httpClientTimeoutSeconds: number
}

/**
 * Read-only settings overview shown on the system monitor screen.
 */
export interface SystemMonitorSettings {
  crm: CrmSettings
  dms: DmsSettings
  email: EmailSettings
}

/**
 * Fetches the CRM, DMS, and Email integration settings for the system monitor screen.
 */
export async function getSystemMonitorSettings(): Promise<SystemMonitorSettings> {
  const response = await apiClient.get<SystemMonitorSettings>('/api/v2/system-monitor/settings')
  return response.data
}