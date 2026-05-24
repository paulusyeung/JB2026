export interface JobNumberParseResult {
  canonicalJobNumbers: string[]
  error: 'unsupported' | null
}

export function parseJobNumberExpression(expression: string): JobNumberParseResult {
  const trimmed = expression.trim()
  if (trimmed.length === 0) {
    return { canonicalJobNumbers: [], error: null }
  }

  const canonicalJobNumbers: string[] = []
  const seen = new Set<string>()

  for (const rawSegment of trimmed.split(',')) {
    const segment = rawSegment.trim()
    if (segment.length === 0) {
      return { canonicalJobNumbers: [], error: 'unsupported' }
    }

    if (segment.includes('/')) {
      const parts = segment.split('/').map((part) => part.trim())
      const first = parts.shift()
      if (!first) {
        return { canonicalJobNumbers: [], error: 'unsupported' }
      }

      const separatorIndex = first.lastIndexOf('-')
      if (separatorIndex <= 0 || separatorIndex >= first.length - 1) {
        return { canonicalJobNumbers: [], error: 'unsupported' }
      }

      const orderNumber = first.slice(0, separatorIndex).trim()
      const firstSuffix = normalizeSuffix(first.slice(separatorIndex + 1))
      if (!orderNumber || firstSuffix === null) {
        return { canonicalJobNumbers: [], error: 'unsupported' }
      }

      pushCanonical(canonicalJobNumbers, seen, `${orderNumber}-${firstSuffix}`)

      for (const suffixPart of parts) {
        const normalizedSuffix = normalizeSuffix(suffixPart)
        if (normalizedSuffix === null) {
          return { canonicalJobNumbers: [], error: 'unsupported' }
        }

        pushCanonical(canonicalJobNumbers, seen, `${orderNumber}-${normalizedSuffix}`)
      }

      continue
    }

    const separatorIndex = segment.lastIndexOf('-')
    if (separatorIndex <= 0 || separatorIndex >= segment.length - 1) {
      return { canonicalJobNumbers: [], error: 'unsupported' }
    }

    const orderNumber = segment.slice(0, separatorIndex).trim()
    const suffix = normalizeSuffix(segment.slice(separatorIndex + 1))
    if (!orderNumber || suffix === null) {
      return { canonicalJobNumbers: [], error: 'unsupported' }
    }

    pushCanonical(canonicalJobNumbers, seen, `${orderNumber}-${suffix}`)
  }

  return { canonicalJobNumbers, error: null }
}

export function buildJobNumberSignature(canonicalJobNumbers: string[]): string {
  return canonicalJobNumbers.join('|')
}

function normalizeSuffix(value: string): number | null {
  const trimmed = value.trim()
  if (!/^\d+$/.test(trimmed)) {
    return null
  }

  const numeric = Number.parseInt(trimmed, 10)
  return Number.isFinite(numeric) && numeric > 0 ? numeric : null
}

function pushCanonical(target: string[], seen: Set<string>, value: string) {
  if (!seen.has(value)) {
    seen.add(value)
    target.push(value)
  }
}