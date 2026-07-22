namespace Lexify.API.Requests.Tts;

public sealed record SpeakRequest(string Text, string LanguageCode);
