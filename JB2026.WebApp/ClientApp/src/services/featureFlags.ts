import type { UiFeatureFlag } from '@/types/api'

export async function getFeatureFlags(): Promise<UiFeatureFlag[]> {
  const response = await fetch('/ui/feature-flags', {
    headers: {
      Accept: 'application/json',
    },
  })

  if (!response.ok) {
    throw new Error(`Failed to load feature flags: ${response.status}`)
  }

  return response.json() as Promise<UiFeatureFlag[]>
}