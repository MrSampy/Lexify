# Piper neural TTS HTTP server (OHF-Voice/piper1-gpl).
# One instance serves all voices — the /synthesize request picks the voice per call.
# Voices are baked into the image because the HTTP server has no on-demand download.
FROM python:3.12-slim AS runtime

# libsndfile1 is required by piper's audio writing; curl for the healthcheck.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libsndfile1 curl \
    && rm -rf /var/lib/apt/lists/*

# Pinned: the HTTP server's synth route (POST /) is version-specific — see PiperTtsService.
RUN pip install --no-cache-dir "piper-tts[http]==1.4.2"

# All 9 app languages, medium quality (Italian uses "paola" medium, not the x_low "riccardo").
# uk/no are lower-resource — quality is verified before rollout; anything dropped here falls back
# to browser TTS automatically on the client.
RUN python -m piper.download_voices \
    en_US-amy-medium \
    de_DE-thorsten-medium \
    fr_FR-siwis-medium \
    es_ES-davefx-medium \
    it_IT-paola-medium \
    pl_PL-gosia-medium \
    ru_RU-ruslan-medium \
    uk_UA-ukrainian_tts-medium \
    no_NO-talesyntese-medium \
    --data-dir /voices

EXPOSE 5000

# -m sets the default voice; requests override it via the "voice" JSON field.
ENTRYPOINT ["python", "-m", "piper.http_server", \
    "--host", "0.0.0.0", "--port", "5000", \
    "--data-dir", "/voices", \
    "-m", "en_US-amy-medium"]
