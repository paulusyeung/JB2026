export function statusIcon(status: number): string {
  if (status >= 3) return 'mdi-flag-check'
  if (status === 2) return 'mdi-flag-outline'
  if (status === 1) return 'mdi-flag-variant-outline'
  return 'mdi-flag-minus-outline'
}

export function statusColor(status: number): string {
  if (status >= 3) return 'success'
  if (status === 2) return 'warning'
  if (status === 1) return 'info'
  return 'secondary'
}

export function statusLabel(status: number): string {
  if (status >= 3) return 'Completed'
  if (status === 2) return 'Paused'
  if (status === 1) return 'In Progress'
  return 'Draft'
}
