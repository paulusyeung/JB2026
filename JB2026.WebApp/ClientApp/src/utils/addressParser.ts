// src/utils/addressParser.ts
//
// Best-effort parser that turns a free-form "Bill To" block (multiline text)
// into structured address fields used by the CRM company form. It is tuned for
// common Hong Kong / international customer address layouts but degrades
// gracefully when the input does not match a known pattern.

import type { CrmAddress } from '@/types/api'

const DEFAULT_COUNTRY = 'Hong Kong'

const KNOWN_COUNTRIES = [
  'Argentina', 'Australia', 'Austria', 'Bangladesh', 'Belgium', 'Brazil', 'Canada',
  'Chile', 'China', 'Colombia', 'Czechia', 'Denmark', 'Egypt', 'Finland', 'France',
  'Germany', 'Greece', 'Hong Kong', 'Hungary', 'Iceland', 'India', 'Indonesia', 'Ireland',
  'Israel', 'Italy', 'Japan', 'Kenya', 'Kuwait', 'Luxembourg', 'Malaysia', 'Mexico',
  'Netherlands', 'New Zealand', 'Nigeria', 'Norway', 'Pakistan', 'Philippines', 'Poland',
  'Portugal', 'Qatar', 'Romania', 'Russia', 'Saudi Arabia', 'Singapore', 'South Africa',
  'South Korea', 'Spain', 'Sweden', 'Switzerland', 'Taiwan', 'Thailand', 'Turkey',
  'Ukraine', 'United Arab Emirates', 'United Kingdom', 'United States', 'Vietnam',
]

const COUNTRY_LOOKUP = new Map(
  KNOWN_COUNTRIES.map(country => [country.toLowerCase(), country]),
)

const HK_DISTRICTS = [
  'hong kong', 'kowloon', 'new territories', 'nt', 'hong kong island', 'hki',
  'kowloon tong', 'tsim sha tsui', 'tsuen wan', 'sha tin', 'shatin', 'tuen mun',
  'yuen long', 'tai po', 'kwun tong', 'kwai chung', 'causeway bay', 'central',
  'wan chai', 'wanchai', 'north point', 'quarry bay', 'chai wan', 'tseung kwan o',
  'tsuen kwan o', 'fanling', 'sheung shui', 'ma on shan', 'lok fu', 'lam tin',
  'diamond hill', 'wong tai sin', 'mong kok', 'mongkok', 'yardley', 'yardley garden',
  'kln', 'hk', 'h.k.', 'h.k',
]

// Lines that start with these labels (case-insensitive) carry contact info
// rather than address parts, e.g. "Attn: John", "Tel: 1234 5678", "Fax: ...".
const CONTACT_LABEL_PATTERN = /^(attn|attention|att|tel|telephone|phone|mobile|fax|email|e-mail)\b[:.\-\s)]*/i

// Matches a phone-like token anywhere in a line (with optional label prefix).
const PHONE_PATTERN = /(\+?\d[\d\s().-]{5,}\d)/

// Country-specific postal code formats (case-insensitive). When a country is
// detected we validate the candidate code against its own pattern rather than
// a generic alphanumeric guess.
const POSTCODE_FORMATS: Record<string, RegExp> = {
  'Hong Kong': /^\d{6}$/,
  'United Kingdom': /^[A-Z]{1,2}\d[A-Z\d]?\s*\d[A-Z]{2}$/i,
  'United States': /^\d{5}(-\d{4})?$/,
  Canada: /^[A-Z]\d[A-Z]\s*\d[A-Z]\d$/i,
  'South Korea': /^\d{5}$/,
  China: /^\d{6}$/,
  Japan: /^\d{3}-?\d{4}$/,
  Singapore: /^\d{6}$/,
  Australia: /^\d{4}$/,
  'New Zealand': /^\d{4}$/,
  Germany: /^\d{5}$/,
  France: /^\d{5}$/,
  Italy: /^\d{5}$/,
  Spain: /^\d{5}$/,
  Portugal: /^\d{4}(?:-\d{3})?$/,
  Netherlands: /^\d{4}\s?[A-Z]{2}$/i,
  Belgium: /^\d{4}$/,
  Switzerland: /^\d{4}$/,
  Austria: /^\d{4}$/,
  Sweden: /^\d{3}\s?\d{2}$/,
  Denmark: /^\d{4}$/,
  Norway: /^\d{4}$/,
  Finland: /^\d{5}$/,
  Poland: /^\d{2}-?\d{3}$/,
  'Czechia': /^\d{3}\s?\d{2}$/,
  Romania: /^\d{6}$/,
  'Russia': /^\d{6}$/,
  Ukraine: /^\d{5}$/,
  India: /^\d{6}$/,
  Indonesia: /^\d{5}$/,
  Malaysia: /^\d{5}$/,
  Philippines: /^\d{4}$/,
  Thailand: /^\d{5}$/,
  'Taiwan': /^\d{3}(\d{2})?$/,
  'South Africa': /^\d{4}$/,
  Brazil: /^\d{5}-?\d{3}$/,
  Mexico: /^\d{5}$/,
  Argentina: /^[A-Z]\d{4}[A-Z]{3}$/i,
  'United Arab Emirates': /^\d{5}$/,
  'Saudi Arabia': /^\d{5}(-\d{4})?$/,
  Vietnam: /^\d{6}$/,
  'Israel': /^\d{5}(\d{2})?$/,
  Egypt: /^\d{5}$/,
  Turkey: /^\d{5}$/,
  Greece: /^\d{3}\s?\d{2}$/,
  Ireland: /^\d{3}\s?([A-Z]{1}\d{2}|\d{4})$/i,
  Luxembourg: /^\d{4}$/,
  'Hungary': /^\d{4}$/,
  Iceland: /^\d{3}$/,
  Qatar: /^\d{5}$/,
  Kuwait: /^\d{5}$/,
  Nigeria: /^\d{6}$/,
  Kenya: /^\d{5}$/,
  Pakistan: /^\d{5}$/,
  Bangladesh: /^\d{4}$/,
  Chile: /^\d{3}(\d{4})?$/,
  Colombia: /^\d{6}$/,
}

// Generic fallback for countries without a known format: 3-10 alphanumerics,
// optionally split by a single space or dash.
const GENERIC_POSTCODE = /^[\dA-Z]{3,10}([\s-][\dA-Z]{1,6})?$/i

function postcodePatternFor(country: string | undefined): RegExp {
  if (country && POSTCODE_FORMATS[country])
    return POSTCODE_FORMATS[country]
  return GENERIC_POSTCODE
}

function isValidPostcode(value: string, country: string | undefined): boolean {
  const pattern = postcodePatternFor(country)
  return pattern.test(value.trim())
}

function isContactLine(line: string): boolean {
  return CONTACT_LABEL_PATTERN.test(line) || PHONE_PATTERN.test(line)
}

function normalizeLine(line: string): string {
  return line.replace(/\s+/g, ' ').trim()
}

function matchCountry(line: string): string | null {
  const lower = line.toLowerCase()
  if (COUNTRY_LOOKUP.has(lower))
    return COUNTRY_LOOKUP.get(lower)!

  // "USA", "UK", "U.K.", "P.R.C." style aliases
  const aliasMap: Record<string, string> = {
    'usa': 'United States',
    'u.s.a.': 'United States',
    'u.s.': 'United States',
    'uk': 'United Kingdom',
    'u.k.': 'United Kingdom',
    'prc': 'China',
    'p.r.c.': 'China',
    'h.k.': 'Hong Kong',
    'hk': 'Hong Kong',
    'kln': 'Hong Kong',
  }

  if (aliasMap[lower])
    return aliasMap[lower]

  // Contains a known country, e.g. "China, Guangdong"
  for (const country of KNOWN_COUNTRIES) {
    if (lower.includes(country.toLowerCase()))
      return country
  }

  return null
}

// Broad candidate match that may look like a postcode fragment. The final
// decision is made by isValidPostcode against the detected country's format.
const POSTCODE_CANDIDATE = /([A-Z0-9]{3,10}(?:[\s-][A-Z0-9]{1,6})?)/i

function extractPostcode(line: string): { postcode: string; rest: string } {
  const match = line.match(POSTCODE_CANDIDATE)
  if (!match)
    return { postcode: '', rest: line }

  const postcode = match[1]?.trim() ?? ''
  const rest = normalizeLine(line.replace(postcode, '')).replace(/^[,\-\s]+|[,\-\s]+$/g, '')
  return { postcode, rest }
}

function isHongKongDistrict(line: string): boolean {
  const lower = line.toLowerCase()
  return HK_DISTRICTS.includes(lower)
}

export interface ParsedBillToAddress extends CrmAddress {
  attn?: string
  tel?: string
  fax?: string
}

export function parseBillToAddress(billTo: string | null | undefined, defaultCountry = DEFAULT_COUNTRY): ParsedBillToAddress {
  const address: ParsedBillToAddress = {
    street1: '',
    street2: '',
    city: '',
    state: '',
    postcode: '',
    country: defaultCountry,
  }

  if (!billTo)
    return address

  const rawLines = billTo
    .split(/\r?\n/)
    .map(normalizeLine)
    .filter(line => line.length > 0)

  if (rawLines.length === 0)
    return address

  const lines = [...rawLines]
  consumeContactLines(lines, address)
  consumeCountry(lines, address)
  consumePostcode(lines, address)

  // Remaining lines are street / city / state.
  const body = lines.filter(line => line.length > 0)

  if (body.length === 1) {
    address.street1 = body[0] ?? ''
  } else if (body.length === 2) {
    address.street1 = body[0] ?? ''
    address.street2 = body[1] ?? ''
  } else if (body.length >= 3) {
    address.street1 = body[0] ?? ''
    // Last body line tends to be city/district; second-to-last state/region.
    const tail = body.slice(1)
    const lastTail = tail[tail.length - 1] ?? ''
    if (isHongKongDistrict(lastTail) || tail.length === 2) {
      address.state = tail[0] ?? ''
      address.city = lastTail
    } else {
      address.street2 = tail.slice(0, -1).join(', ')
      address.city = lastTail
    }
  }

  if (!address.country)
    address.country = defaultCountry

  return address
}

function consumeContactLines(lines: string[], address: ParsedBillToAddress): void {
  for (let i = lines.length - 1; i >= 0; i--) {
    const line = lines[i] ?? ''
    if (!isContactLine(line))
      continue

    const lower = line.toLowerCase()
    const value = normalizeLine(line.replace(CONTACT_LABEL_PATTERN, ''))

    if (lower.startsWith('attn') || lower.startsWith('attention')) {
      address.attn = value || address.attn
    } else if (lower.startsWith('fax')) {
      address.fax = value || address.fax
    } else if (lower.startsWith('tel') || lower.startsWith('telephone') || lower.startsWith('phone') || lower.startsWith('mobile')) {
      address.tel = value || address.tel
    } else if (PHONE_PATTERN.test(line)) {
      // Bare phone number line without a label — treat as telephone.
      address.tel = address.tel || (line.match(PHONE_PATTERN)?.[1]?.trim() ?? '')
    }

    lines.splice(i, 1)
  }
}

function consumeCountry(lines: string[], address: CrmAddress): boolean {
  for (let i = 0; i < lines.length; i++) {
    const line = lines[i] ?? ''
    const found = matchCountry(line)
    if (found) {
      address.country = found
      lines.splice(i, 1)
      return true
    }
  }
  return false
}

function consumePostcode(lines: string[], address: ParsedBillToAddress): void {
  const country = address.country
  for (let i = 0; i < lines.length; i++) {
    const line = lines[i] ?? ''
    const { postcode, rest } = extractPostcode(line)
    if (postcode && isValidPostcode(postcode, country)) {
      address.postcode = postcode
      lines[i] = rest
      return
    }
  }
}
