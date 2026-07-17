export const env = {
  API_URL: import.meta.env.VITE_API_URL as string,
  API_TIMEOUT_MS: Number(import.meta.env.VITE_API_TIMEOUT_MS ?? 10000),
  // Deploy-time hard switch for server-side neural TTS. Off → the client uses only browser speech.
  // Runtime on/off (per environment) is the server's features.tts_enabled setting.
  TTS_ENABLED: import.meta.env.VITE_TTS_ENABLED !== 'false',
} as const
