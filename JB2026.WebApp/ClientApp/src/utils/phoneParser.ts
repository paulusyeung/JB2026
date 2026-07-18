import {
  AsYouType,
  parsePhoneNumberFromString,
  type CountryCode,
} from 'libphonenumber-js'

export { parsePhoneNumberFromString }

export interface CountryCallingCode {
  code: string
  label: string
}

export const callingCodeOptions: CountryCallingCode[] = [
  { code: 'US', label: '+1 (US/CA)' },
  { code: 'GB', label: '+44 (UK)' },
  { code: 'HK', label: '+852 (HK)' },
  { code: 'CN', label: '+86 (CN)' },
  { code: 'JP', label: '+81 (JP)' },
  { code: 'SG', label: '+65 (SG)' },
  { code: 'AU', label: '+61 (AU)' },
  { code: 'TW', label: '+886 (TW)' },
]

/** Strips everything except digits. */
export function toDigits(value: string): string {
  return (value || '').replace(/\D/g, '')
}

// Calling-code (digits only) -> ISO country, derived from the option list so it
// stays in sync with the supported countries. Sorted longest-first for matching.
const callingCodeToCountry: { code: string; country: string }[] = callingCodeOptions
  .map(option => ({
    code: (option.code === 'US' ? '1'
      : option.code === 'GB' ? '44'
      : option.code === 'HK' ? '852'
      : option.code === 'CN' ? '86'
      : option.code === 'JP' ? '81'
      : option.code === 'SG' ? '65'
      : option.code === 'AU' ? '61'
      : option.code === 'TW' ? '886'
      : ''),
    country: option.code,
  }))
  .filter(entry => entry.code !== '')
  .sort((a, b) => b.code.length - a.code.length)

/**
 * Infers the country from a leading international calling code embedded in the
 * dialed digits (e.g. "85212345678" -> "HK"), so users can type a number without
 * a "+" prefix. Returns undefined when no known prefix matches.
 */
export function countryFromCallingCodePrefix(digits: string): string | undefined {
  const match = callingCodeToCountry.find(entry => digits.startsWith(entry.code))
  return match?.country
}

/**
 * Builds an E.164 phone string (e.g. "+14155552671") from a country code and a
 * national number (digits only). Returns an empty string when either part is
 * missing.
 */
export function toE164(country: string, nationalDigits: string): string {
  const trimmedCountry = (country || '').trim()
  const trimmedDigits = toDigits(nationalDigits)
  if (!trimmedCountry || !trimmedDigits)
    return ''
  const parsed = parsePhoneNumberFromString(trimmedDigits, trimmedCountry as CountryCode)
  return parsed?.isValid() ? parsed.number : ''
}

export interface ParsePhoneResult {
  country: string
  nationalDigits: string
  e164: string
}

/**
 * Parses a possibly-formatted phone string into its country, national digits,
 * and E.164 representation. Handles both "+1 4155552671" and "+14155552671"
 * forms. When no country can be inferred, country is empty.
 */
export function parsePhone(raw: string): ParsePhoneResult {
  const trimmed = (raw || '').trim()
  const parsed = parsePhoneNumberFromString(trimmed)
  if (parsed?.isValid()) {
    return {
      country: parsed.country ?? '',
      nationalDigits: parsed.nationalNumber,
      e164: parsed.number,
    }
  }

  return { country: '', nationalDigits: toDigits(trimmed), e164: '' }
}

export interface PhoneValidationResult {
  valid: boolean
  message?: string
  /** E.164 form of the number when valid. */
  e164?: string
}

/**
 * Validates a phone number for the given country using libphonenumber-js, which
 * enforces real national-number rules including the area code. `formatMessage`
 * is used when the number is non-empty but invalid so the caller can localize
 * the error text.
 */
export function validateNationalNumber(
  country: string,
  value: string,
  formatMessage: (args: { country: string }) => string,
): PhoneValidationResult {
  const trimmed = (value || '').trim()
  if (!trimmed)
    return { valid: true }

  const parsed = parsePhoneNumberFromString(trimmed, country as CountryCode)
  if (parsed?.isValid()) {
    return { valid: true, e164: parsed.number }
  }

  return { valid: false, message: formatMessage({ country }) }
}

/** Formats a national number as the user types, using the selected country. */
export function formatPartialNumber(country: string, value: string): string {
  return new AsYouType(country as CountryCode).input(value)
}

/**
 * Returns a nicely grouped display form of a phone number, e.g. "+852 8227 9606".
 * Accepts E.164 ("+85282279606") or a "+852 82279606" style string and falls back
 * to the input unchanged when it cannot be parsed.
 */
export function formatPhoneDisplay(raw: string): string {
  const trimmed = (raw || '').trim()
  if (!trimmed)
    return trimmed

  const parsed = parsePhoneNumberFromString(trimmed)
  if (parsed?.isValid())
    return parsed.formatInternational()

  return trimmed
}
