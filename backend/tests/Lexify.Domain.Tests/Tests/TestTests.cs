using Lexify.Domain.Common;
using Lexify.Domain.Entities;

namespace Lexify.Domain.Tests.Tests;

public class TestTests
{
    [Fact]
    public void Create_WithEmptyUserId_ThrowsDomainException()
    {
        var ex = Assert.Throws<DomainException>(() => Test.Create(Guid.Empty, "My Test"));
        Assert.Equal("User ID cannot be empty.", ex.Message);
    }

    [Fact]
    public void Create_WithEmptyTitle_ThrowsDomainException()
    {
        var ex = Assert.Throws<DomainException>(() => Test.Create(Guid.NewGuid(), "   "));
        Assert.Equal("Test title cannot be empty.", ex.Message);
    }

    [Fact]
    public void Create_WithValidData_StatusIsGenerating()
    {
        var test = Test.Create(Guid.NewGuid(), "Vocab Quiz");
        Assert.Equal(Test.Statuses.Generating, test.Status);
        Assert.Null(test.QuestionCount);
    }

    [Fact]
    public void MarkReady_FromGenerating_SetsStatusAndQuestionCount()
    {
        var test = Test.Create(Guid.NewGuid(), "Vocab Quiz");
        test.MarkReady(10);
        Assert.Equal(Test.Statuses.Ready, test.Status);
        Assert.Equal(10, test.QuestionCount);
    }

    [Fact]
    public void MarkReady_FromReadyAgain_ThrowsDomainException()
    {
        var test = Test.Create(Guid.NewGuid(), "Vocab Quiz");
        test.MarkReady(5);
        var ex = Assert.Throws<DomainException>(() => test.MarkReady(5));
        Assert.Equal("Only a generating test can be marked as ready.", ex.Message);
    }

    [Fact]
    public void MarkReady_FromArchived_ThrowsDomainException()
    {
        var test = Test.Create(Guid.NewGuid(), "Vocab Quiz");
        test.Archive();
        var ex = Assert.Throws<DomainException>(() => test.MarkReady(5));
        Assert.Equal("Only a generating test can be marked as ready.", ex.Message);
    }

    [Fact]
    public void Archive_FromGenerating_SetsStatusArchived()
    {
        var test = Test.Create(Guid.NewGuid(), "Vocab Quiz");
        test.Archive();
        Assert.Equal(Test.Statuses.Archived, test.Status);
    }

    [Fact]
    public void Archive_FromReady_SetsStatusArchived()
    {
        var test = Test.Create(Guid.NewGuid(), "Vocab Quiz");
        test.MarkReady(5);
        test.Archive();
        Assert.Equal(Test.Statuses.Archived, test.Status);
    }

    [Fact]
    public void Archive_WhenAlreadyArchived_ThrowsDomainException()
    {
        var test = Test.Create(Guid.NewGuid(), "Vocab Quiz");
        test.Archive();
        var ex = Assert.Throws<DomainException>(() => test.Archive());
        Assert.Equal("Test is already archived.", ex.Message);
    }
}
