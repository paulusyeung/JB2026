export const authMessages = {
      eyebrow: 'Slice B foundation',
      title: 'API-authenticated sign in',
      description: 'The web app uses JWT bearer tokens from the ASP.NET Core API and persists them for subsequent slice navigation.',
      username: 'Username',
      password: 'Password',
      signIn: 'Sign In',
      useDevDefaults: 'Use Dev Defaults',
      errors: {
        authenticationFailed: 'Authentication failed. Verify the configured API credentials.',
        apiUnavailable: 'The API is unreachable. Start JB2026.Api and try again.',
      },
    } as const
