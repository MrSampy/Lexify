namespace Lexify.Domain.Entities;

public sealed class TestBlock
{
    public Guid TestId { get; private set; }
    public Guid BlockId { get; private set; }

    private TestBlock() { }

    public TestBlock(Guid testId, Guid blockId)
    {
        TestId = testId;
        BlockId = blockId;
    }
}
