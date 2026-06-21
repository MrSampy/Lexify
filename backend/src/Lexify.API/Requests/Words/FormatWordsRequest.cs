namespace Lexify.API.Requests.Words;

public sealed record FormatWordsRequest(
    string RawText,
    string TargetLanguage,
    string NativeLanguage);
