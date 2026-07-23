namespace Lexify.Application.Abstractions;

/// <summary>
/// Persists feedback attachment bytes outside the database. Implementations own naming: callers pass
/// only the file extension and get back an opaque storage name, so a user-supplied filename never
/// reaches a filesystem path.
/// </summary>
public interface IFeedbackAttachmentStorage
{
    /// <param name="extension">Extension including the dot, derived from the sniffed content type.</param>
    /// <returns>The generated storage name to persist on the attachment row.</returns>
    Task<string> SaveAsync(byte[] content, string extension, CancellationToken ct = default);

    /// <summary>The stored bytes, or null when the file is missing (e.g. volume reset).</summary>
    Task<byte[]?> ReadAsync(string storageName, CancellationToken ct = default);

    /// <summary>Best-effort delete; never throws when the file is already gone.</summary>
    Task DeleteAsync(string storageName, CancellationToken ct = default);
}
