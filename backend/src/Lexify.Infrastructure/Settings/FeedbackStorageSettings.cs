namespace Lexify.Infrastructure.Settings;

public sealed class FeedbackStorageSettings
{
    /// <summary>Directory (a mounted volume in prod) where feedback attachments are written.</summary>
    public string Directory { get; init; } = "feedback-uploads";
}
