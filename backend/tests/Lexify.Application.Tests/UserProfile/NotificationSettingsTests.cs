using Lexify.Application.Abstractions;
using Lexify.Application.Common;
using Lexify.Application.Notifications.Commands.Unsubscribe;
using Lexify.Application.UserProfile.Commands.UpdateNotificationSettings;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using NSubstitute;

namespace Lexify.Application.Tests.UserProfile;

public class NotificationSettingsTests
{
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IUnsubscribeTokenService _tokens = Substitute.For<IUnsubscribeTokenService>();

    private static User CreateUser() => User.Create("user@example.com", "hash", "Test User");

    // ── profile toggle ───────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateNotificationSettings_TurnsRemindersOff()
    {
        var user = CreateUser();
        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        var handler = new UpdateNotificationSettingsCommandHandler(_userRepo, _unitOfWork);

        var result = await handler.Handle(
            new UpdateNotificationSettingsCommand(user.Id, false), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(user.EmailRemindersEnabled);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateNotificationSettings_UnknownUser_ReturnsNotFound()
    {
        _userRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);
        var handler = new UpdateNotificationSettingsCommandHandler(_userRepo, _unitOfWork);

        var result = await handler.Handle(
            new UpdateNotificationSettingsCommand(Guid.NewGuid(), false), CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── unsubscribe link ─────────────────────────────────────────────────────

    [Fact]
    public async Task Unsubscribe_ValidToken_TurnsRemindersOff()
    {
        var user = CreateUser();
        GivenTokenResolvesTo("good-token", user.Id);
        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var result = await CreateUnsubscribeHandler()
            .Handle(new UnsubscribeCommand("good-token"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(user.EmailRemindersEnabled);
    }

    [Fact]
    public async Task Unsubscribe_TwiceWithTheSameToken_StaysSuccessful()
    {
        var user = CreateUser();
        GivenTokenResolvesTo("good-token", user.Id);
        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        var handler = CreateUnsubscribeHandler();

        await handler.Handle(new UnsubscribeCommand("good-token"), CancellationToken.None);
        var second = await handler.Handle(new UnsubscribeCommand("good-token"), CancellationToken.None);

        // The link lives in an inbox forever; re-opening it must not look like an error.
        Assert.True(second.IsSuccess);
        Assert.False(user.EmailRemindersEnabled);
    }

    [Fact]
    public async Task Unsubscribe_TamperedToken_IsRejectedWithoutTouchingTheDatabase()
    {
        _tokens.TryValidate("forged", out Arg.Any<Guid>()).Returns(false);

        var result = await CreateUnsubscribeHandler()
            .Handle(new UnsubscribeCommand("forged"), CancellationToken.None);

        Assert.Equal(ResultStatus.Failure, result.Status);
        await _userRepo.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Unsubscribe_TokenForADeletedUser_LooksLikeAnInvalidLink()
    {
        var missingUserId = Guid.NewGuid();
        GivenTokenResolvesTo("orphan-token", missingUserId);
        _userRepo.GetByIdAsync(missingUserId, Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await CreateUnsubscribeHandler()
            .Handle(new UnsubscribeCommand("orphan-token"), CancellationToken.None);

        // Same message as a forged token — the endpoint is anonymous and must not confirm accounts.
        Assert.Equal(ResultStatus.Failure, result.Status);
        Assert.Equal("This unsubscribe link is not valid.", result.ErrorMessage);
    }

    private UnsubscribeCommandHandler CreateUnsubscribeHandler() =>
        new(_tokens, _userRepo, _unitOfWork);

    private void GivenTokenResolvesTo(string token, Guid userId) =>
        _tokens.TryValidate(token, out Arg.Any<Guid>())
            .Returns(call =>
            {
                call[1] = userId;
                return true;
            });
}
