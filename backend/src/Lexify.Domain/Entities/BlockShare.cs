using Lexify.Domain.Common;

namespace Lexify.Domain.Entities;

/// <summary>
/// A shareable link to a word block. The recipient gets a read-only preview and can copy the block
/// into their own account; the two copies are then independent, because every word carries its own
/// SM-2 progress and sharing a row would mean sharing a study history.
/// </summary>
/// <remarks>
/// A stored row rather than a signed stateless token (as used for unsubscribe links) because the owner
/// needs to see whether sharing is on and to revoke a link that is already out in the world — neither
/// is expressible in a self-contained token.
/// </remarks>
public sealed class BlockShare : BaseEntity
{
    public Guid BlockId { get; private set; }
    public Guid OwnerUserId { get; private set; }
    public string Token { get; private set; } = default!;
    public DateTimeOffset? RevokedAt { get; private set; }
    public int ViewCount { get; private set; }
    public int CopyCount { get; private set; }

    private BlockShare() { }

    public BlockShare(Guid blockId, Guid ownerUserId, string token)
    {
        if (blockId == Guid.Empty) throw new DomainException("Block ID cannot be empty.");
        if (ownerUserId == Guid.Empty) throw new DomainException("Owner user ID cannot be empty.");
        if (string.IsNullOrWhiteSpace(token)) throw new DomainException("Share token cannot be empty.");

        BlockId = blockId;
        OwnerUserId = ownerUserId;
        Token = token;
    }

    public bool IsActive => RevokedAt is null;

    public void Revoke()
    {
        if (RevokedAt is not null) return;
        RevokedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RecordView()
    {
        ViewCount++;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RecordCopy()
    {
        CopyCount++;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
