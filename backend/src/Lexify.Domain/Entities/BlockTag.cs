namespace Lexify.Domain.Entities;

public sealed class BlockTag
{
    public Guid BlockId { get; private set; }
    public int TagId { get; private set; }

    private BlockTag() { }

    public BlockTag(Guid blockId, int tagId)
    {
        BlockId = blockId;
        TagId = tagId;
    }
}
