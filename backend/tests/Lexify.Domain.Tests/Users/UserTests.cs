using Lexify.Domain.Entities;

namespace Lexify.Domain.Tests.Users;

public class UserTests
{
    private static readonly DateTimeOffset LongAgo = DateTimeOffset.UtcNow.AddDays(-7);

    /// <summary>A user whose UpdatedAt is old enough that a real write is unmistakable.</summary>
    private static User CreateUser()
    {
        var user = User.Create("user@example.com", "hash", "Test User");
        user.UpdatedAt = LongAgo;
        return user;
    }

    [Fact]
    public void EmailReminders_AreOnByDefault()
    {
        Assert.True(CreateUser().EmailRemindersEnabled);
    }

    [Fact]
    public void SetEmailReminders_False_TurnsThemOffAndTouchesUpdatedAt()
    {
        var user = CreateUser();

        user.SetEmailReminders(false);

        Assert.False(user.EmailRemindersEnabled);
        Assert.True(user.UpdatedAt > LongAgo);
    }

    [Fact]
    public void SetEmailReminders_ToTheCurrentValue_IsANoOp()
    {
        var user = CreateUser();

        // Unsubscribing twice (a second click on the link in an old email) must not look like an edit.
        user.SetEmailReminders(true);

        Assert.True(user.EmailRemindersEnabled);
        Assert.Equal(LongAgo, user.UpdatedAt);
    }

    [Fact]
    public void SetEmailReminders_CanBeTurnedBackOn()
    {
        var user = CreateUser();

        user.SetEmailReminders(false);
        user.SetEmailReminders(true);

        Assert.True(user.EmailRemindersEnabled);
    }
}
