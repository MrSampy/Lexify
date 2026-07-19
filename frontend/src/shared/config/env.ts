export const env = {
  // Defaults to '' so same-origin deploys work: the production image is built WITHOUT VITE_API_URL
  // (see .github/workflows/deploy.yml) and nginx proxies /api/* to the backend on the same origin.
  // The fallback is not cosmetic — axios treats a `undefined` baseURL as "use relative URLs", but a
  // template literal stringifies it, so `${env.API_URL}/api/words/format` became the relative path
  // "undefined/api/words/format". That misses nginx's `location /api/`, lands in the SPA fallback,
  // and a POST to a static file answers 405 — the request never reached the backend at all.
  API_URL: (import.meta.env.VITE_API_URL as string | undefined) ?? '',
  API_TIMEOUT_MS: Number(import.meta.env.VITE_API_TIMEOUT_MS ?? 10000),
  // Deploy-time hard switch for server-side neural TTS. Off → the client uses only browser speech.
  // Runtime on/off (per environment) is the server's features.tts_enabled setting.
  TTS_ENABLED: import.meta.env.VITE_TTS_ENABLED !== 'false',
} as const
