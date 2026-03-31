# Translation Guide

This guide keeps the locale files easy to review and safe to edit.

## Scope

Locale folders:
- `en/`
- `zhHans/`
- `zhHant/`

Each folder has domain files such as:
- `auth.ts`
- `dashboard.ts`
- `jobs.ts`
- `settings.ts`

## Rules

1. Do not rename or remove keys.
2. Keep key order exactly the same as English (`en`).
3. Keep placeholder tokens unchanged, for example `{name}`, `{count}`, `{amount}`, `{date}`, `{qty}`.
4. Keep punctuation and sentence style consistent within a language.
5. If a term appears in multiple files, translate it the same way each time.

## Do/Do Not examples

### Keys

Do:

```ts
export const authMessages = {
  title: 'Sign In',
  username: 'Username',
}
```

Do not:

```ts
export const authMessages = {
  loginTitle: 'Sign In', // changed key name
  username: 'Username',
}
```

### Placeholder tokens

Do:

```ts
signedInAs: 'Signed in as {name} ({role})'
rows: 'Rows: {count}'
```

Do not:

```ts
signedInAs: 'Signed in as {username} ({role})' // changed token
rows: 'Rows: %count%' // changed format
```

### Key order

Do:

```ts
title: '...'
subtitle: '...'
loadFailed: '...'
```

Do not:

```ts
loadFailed: '...'
title: '...'
subtitle: '...'
```

## Suggested workflow for new text

1. Add new key/value in `en/<domain>.ts`.
2. Copy the same key into `zhHans/<domain>.ts` and `zhHant/<domain>.ts`.
3. Translate only the value, not the key.
4. Verify placeholder tokens are unchanged.
5. Run type check in `ClientApp`:

```bash
npm run typecheck
```

## Consistency notes for this project

- Keep route labels short and menu-friendly.
- Keep status labels concise (for tables and chips).
- Preserve API and product names as-is (for example `JB2026`, `API`, `JWT`, `SPA`, `SML`, `CKEditor`).
