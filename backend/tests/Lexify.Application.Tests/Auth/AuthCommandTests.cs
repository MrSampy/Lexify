using System.Reflection;
using Lexify.Application.Abstractions;
using Lexify.Application.Auth.Commands.Common;
using Lexify.Application.Auth.Commands.ForgotPassword;
using Lexify.Application.Auth.Commands.Login;
using Lexify.Application.Auth.Commands.RefreshToken;
using Lexify.Application.Auth.Commands.Register;
using Lexify.Application.Auth.Commands.ResetPassword;
using Lexify.Application.Auth.Common;
using Lexify.Application.Common;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using NSubstitute;

namespace Lexify.Application.Tests.Auth;

public class AuthCommandTests
{
    // ---- Register ----

    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IBackgroundJobService _backgroundJobs = Substitute.For<IBackgroundJobService>();
    private readonly ISystemSettingRepository _settingRepo = Substitute.For<ISystemSettingRepository>();
    private readonly IEmailVerificationService _emailVerification =
        Substitute.For<IEmailVerificationService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    public AuthCommandTests()
    {
        // Hashing is not what the Register tests are about; individual tests override this when they
        // assert on the stored hash.
        _passwordHasher.Hash(Arg.Any<string>()).Returns("hashed");

        // These tests predate email confirmation and cover the registration gate itself; keep them on
        // the "confirmation off" path. The confirmation flow has its own integration tests.
        _emailVerification.IsRequiredAsync(Arg.Any<CancellationToken>()).Returns(false);

        // Likewise the classic login tests predate 2FA — default them to the no-second-factor path.
        _twoFactor.IsRequiredForAsync(Arg.Any<User>(), Arg.Any<CancellationToken>()).Returns(false);
    }

    private RegisterCommandHandler CreateRegisterHandler() =>
        new(_userRepo, _settingRepo, _passwordHasher, _emailVerification, _unitOfWork, _backgroundJobs);

    /// <summary>Points the settings repo at a given registration_enabled / invite_code pair.</summary>
    private void GivenRegistrationSettings(string registrationEnabled, string inviteCode)
    {
        _settingRepo.GetByKeyAsync(SystemSetting.Keys.RegistrationEnabled, Arg.Any<CancellationToken>())
            .Returns(new SystemSetting(SystemSetting.Keys.RegistrationEnabled, registrationEnabled, "bool"));
        _settingRepo.GetByKeyAsync(SystemSetting.Keys.InviteCode, Arg.Any<CancellationToken>())
            .Returns(new SystemSetting(SystemSetting.Keys.InviteCode, inviteCode, "string"));
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsFailure()
    {
        var existingUser = new User("user@example.com", "hash");
        _userRepo.GetByEmailAsync("user@example.com", Arg.Any<CancellationToken>())
            .Returns(existingUser);

        var handler = CreateRegisterHandler();
        var result = await handler.Handle(
            new RegisterCommand("user@example.com", "password", null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Failure, result.Status);
        _backgroundJobs.DidNotReceive().EnqueueWelcomeEmail(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Register_NewEmail_ReturnsOkWithGuid()
    {
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);
        _passwordHasher.Hash("password").Returns("hashed");

        var handler = CreateRegisterHandler();
        var result = await handler.Handle(
            new RegisterCommand("new@example.com", "password", "Alice"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
        _backgroundJobs.Received(1).EnqueueWelcomeEmail("new@example.com", "Alice");
    }

    [Fact]
    public async Task Register_NoDisplayName_WelcomeEmailUsesEmailPrefix()
    {
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);
        _passwordHasher.Hash("password").Returns("hashed");

        var handler = CreateRegisterHandler();
        await handler.Handle(
            new RegisterCommand("bob@example.com", "password", null),
            CancellationToken.None);

        _backgroundJobs.Received(1).EnqueueWelcomeEmail("bob@example.com", "bob");
    }

    // ---- Register: invite-only gate ----

    [Fact]
    public async Task Register_RegistrationOpen_IgnoresInviteCode()
    {
        GivenRegistrationSettings(registrationEnabled: "true", inviteCode: "SECRET");
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await CreateRegisterHandler().Handle(
            new RegisterCommand("new@example.com", "password", "Alice", InviteCode: null),
            CancellationToken.None);

        // An invite code exists, but while sign-up is open it must not be demanded.
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Register_RegistrationClosed_CorrectInviteCode_Succeeds()
    {
        GivenRegistrationSettings(registrationEnabled: "false", inviteCode: "SECRET");
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await CreateRegisterHandler().Handle(
            new RegisterCommand("new@example.com", "password", "Alice", InviteCode: "SECRET"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        _backgroundJobs.Received(1).EnqueueWelcomeEmail("new@example.com", "Alice");
    }

    [Theory]
    [InlineData(null)]        // no code supplied
    [InlineData("")]          // blank must not pass as "no code required"
    [InlineData("WRONG")]     // wrong code
    [InlineData("secret")]    // codes are case-sensitive
    public async Task Register_RegistrationClosed_BadInviteCode_ReturnsFailure(string? code)
    {
        GivenRegistrationSettings(registrationEnabled: "false", inviteCode: "SECRET");
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await CreateRegisterHandler().Handle(
            new RegisterCommand("new@example.com", "password", "Alice", InviteCode: code),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Failure, result.Status);
        await _userRepo.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        _backgroundJobs.DidNotReceive().EnqueueWelcomeEmail(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Register_RegistrationClosed_NoInviteCodeSet_ClosesSignUpEntirely()
    {
        // Empty code must not degrade into "any blank code works" — that would silently reopen sign-up.
        GivenRegistrationSettings(registrationEnabled: "false", inviteCode: "");
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await CreateRegisterHandler().Handle(
            new RegisterCommand("new@example.com", "password", "Alice", InviteCode: ""),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("closed", result.ErrorMessage);
        await _userRepo.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Register_SettingRowMissing_DefaultsToOpen()
    {
        // A fresh install with no seeded row must not lock everyone out.
        _settingRepo.GetByKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((SystemSetting?)null);
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await CreateRegisterHandler().Handle(
            new RegisterCommand("new@example.com", "password", "Alice"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    // ---- Login ----

    private readonly IRefreshTokenRepository _refreshTokenRepo =
        Substitute.For<IRefreshTokenRepository>();
    private readonly IJwtService _jwtService = Substitute.For<IJwtService>();
    private readonly ITwoFactorService _twoFactor = Substitute.For<ITwoFactorService>();
    private readonly IAuthSessionService _authSession = Substitute.For<IAuthSessionService>();

    private LoginCommandHandler CreateLoginHandler() =>
        new(_userRepo, _passwordHasher, _emailVerification, _twoFactor, _jwtService, _authSession);

    [Fact]
    public async Task Login_UserNotFound_ReturnsFailure()
    {
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var handler = CreateLoginHandler();
        var result = await handler.Handle(
            new LoginCommand("missing@example.com", "password"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid credentials.", result.ErrorMessage);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsFailure()
    {
        var user = new User("user@example.com", "correcthash");
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("wrongpass", "correcthash").Returns(false);

        var handler = CreateLoginHandler();
        var result = await handler.Handle(
            new LoginCommand("user@example.com", "wrongpass"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid credentials.", result.ErrorMessage);
    }

    [Fact]
    public async Task Login_SuspendedUser_ReturnsFailure()
    {
        var user = new User("user@example.com", "hash", status: User.Statuses.Suspended);
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        var handler = CreateLoginHandler();
        var result = await handler.Handle(
            new LoginCommand("user@example.com", "pass"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Account is not active.", result.ErrorMessage);
    }

    [Fact]
    public async Task Login_ValidCredentials_NoTwoFactor_ReturnsAuthenticatedSession()
    {
        var user = new User("user@example.com", "hash");
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _authSession.IssueAsync(user, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new AuthResponse("access_token", "refresh_token", DateTimeOffset.UtcNow.AddHours(1)));

        var handler = CreateLoginHandler();
        var result = await handler.Handle(
            new LoginCommand("user@example.com", "pass"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.TwoFactorRequired);
        Assert.Equal("access_token", result.Value.Session!.AccessToken);
        Assert.Equal("refresh_token", result.Value.Session.RefreshToken);
    }

    [Fact]
    public async Task Login_TwoFactorRequired_ReturnsChallengeAndIssuesCode_NoSession()
    {
        var user = new User("admin@example.com", "hash", role: User.Roles.Admin);
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _twoFactor.IsRequiredForAsync(user, Arg.Any<CancellationToken>()).Returns(true);
        _jwtService.GenerateTwoFactorChallengeToken(user.Id).Returns("challenge_token");

        var handler = CreateLoginHandler();
        var result = await handler.Handle(
            new LoginCommand("admin@example.com", "pass"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.TwoFactorRequired);
        Assert.Equal("challenge_token", result.Value.TwoFactorChallenge);
        Assert.Null(result.Value.Session);
        await _twoFactor.Received(1).IssueCodeAsync(user, Arg.Any<CancellationToken>());
        // No session must be minted on the challenge path.
        await _authSession.DidNotReceive().IssueAsync(
            Arg.Any<User>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    // ---- RefreshToken ----

    [Fact]
    public async Task RefreshToken_NullToken_ReturnsFailure()
    {
        _refreshTokenRepo.GetByHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((RefreshToken?)null);

        var handler = new RefreshTokenCommandHandler(_userRepo, _refreshTokenRepo, _jwtService);
        var result = await handler.Handle(
            new RefreshTokenCommand("unknowntoken"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        // The message is the machine-readable marker the API layer keys the cookie-clearing decision on.
        Assert.Equal(AuthErrorCodes.RefreshTokenDead, result.ErrorMessage);
    }

    [Fact]
    public async Task RefreshToken_RevokedToken_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var token = new RefreshToken(userId, "hash", DateTimeOffset.UtcNow.AddDays(30));
        token.Revoke(); // RevokedAt is now set → IsActive = false

        _refreshTokenRepo.GetByHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(token);

        var handler = new RefreshTokenCommandHandler(_userRepo, _refreshTokenRepo, _jwtService);
        var result = await handler.Handle(
            new RefreshTokenCommand("revokedtoken"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        // The message is the machine-readable marker the API layer keys the cookie-clearing decision on.
        Assert.Equal(AuthErrorCodes.RefreshTokenDead, result.ErrorMessage);
    }

    [Fact]
    public async Task RefreshToken_ExpiredToken_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var token = new RefreshToken(userId, "hash", DateTimeOffset.UtcNow.AddDays(-1)); // expired

        _refreshTokenRepo.GetByHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(token);

        var handler = new RefreshTokenCommandHandler(_userRepo, _refreshTokenRepo, _jwtService);
        var result = await handler.Handle(
            new RefreshTokenCommand("expiredtoken"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        // The message is the machine-readable marker the API layer keys the cookie-clearing decision on.
        Assert.Equal(AuthErrorCodes.RefreshTokenDead, result.ErrorMessage);
    }

    [Fact]
    public async Task RefreshToken_ValidToken_ReturnsOkWithNewTokens()
    {
        var userId = Guid.NewGuid();
        var user = new User("user@example.com", "hash");
        var token = new RefreshToken(userId, "hash", DateTimeOffset.UtcNow.AddDays(30));

        _refreshTokenRepo.GetByHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(token);
        _userRepo.GetByIdAsync(token.UserId, Arg.Any<CancellationToken>()).Returns(user);
        _jwtService.GenerateAccessToken(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns("new_access_token");
        _jwtService.GetExpiry().Returns(DateTimeOffset.UtcNow.AddHours(1));

        var handler = new RefreshTokenCommandHandler(_userRepo, _refreshTokenRepo, _jwtService);
        var result = await handler.Handle(
            new RefreshTokenCommand("validtoken"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("new_access_token", result.Value!.AccessToken);
        Assert.NotNull(result.Value.RefreshToken);
    }

    [Fact]
    public async Task RefreshToken_JustRotatedToken_ReturnsSessionWithoutRotatingAgain()
    {
        // Two tabs refreshing at once: the loser presents the token the winner just rotated away.
        // Inside the grace window that must still yield a session, or one unlucky race signs the user
        // out of a healthy session.
        var userId = Guid.NewGuid();
        var user = new User("user@example.com", "hash");
        var presented = new RefreshToken(userId, "old_hash", DateTimeOffset.UtcNow.AddDays(30));
        var successor = presented.Rotate("new_hash", DateTimeOffset.UtcNow.AddDays(30));

        _refreshTokenRepo.GetByHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(presented);
        _refreshTokenRepo.GetByIdAsync(successor.Id, Arg.Any<CancellationToken>()).Returns(successor);
        _userRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);
        _jwtService.GenerateAccessToken(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns("new_access_token");
        _jwtService.GetExpiry().Returns(DateTimeOffset.UtcNow.AddHours(1));

        var handler = new RefreshTokenCommandHandler(_userRepo, _refreshTokenRepo, _jwtService);
        var result = await handler.Handle(
            new RefreshTokenCommand("just_rotated"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("new_access_token", result.Value!.AccessToken);
        // Null = "leave the cookie alone": the successor the winner wrote is the live one, and writing
        // another value here could clobber it if the responses land out of order.
        Assert.Null(result.Value.RefreshToken);
        await _refreshTokenRepo.DidNotReceive()
            .AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshToken_RotatedLongAgo_IsRefused()
    {
        // Same shape as the grace case, but the rotation is old: this is a replay of a token that has
        // been superseded for a while, not a race, so it must not be honoured.
        var userId = Guid.NewGuid();
        var presented = new RefreshToken(userId, "old_hash", DateTimeOffset.UtcNow.AddDays(30));
        var successor = presented.Rotate("new_hash", DateTimeOffset.UtcNow.AddDays(30));
        // The entity deliberately offers no way to backdate a revocation, so reach for the backing
        // field — same approach the test suite already uses to arrange private collection state.
        typeof(RefreshToken)
            .GetField($"<{nameof(RefreshToken.RevokedAt)}>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(presented, DateTimeOffset.UtcNow.AddMinutes(-5));

        _refreshTokenRepo.GetByHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(presented);
        _refreshTokenRepo.GetByIdAsync(successor.Id, Arg.Any<CancellationToken>()).Returns(successor);

        var handler = new RefreshTokenCommandHandler(_userRepo, _refreshTokenRepo, _jwtService);
        var result = await handler.Handle(
            new RefreshTokenCommand("stale_rotated"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthErrorCodes.RefreshTokenDead, result.ErrorMessage);
    }

    [Fact]
    public async Task RefreshToken_JustRotatedButSuccessorRevoked_IsRefused()
    {
        // The whole chain was killed (logout, password change) right after a rotation. The grace window
        // must not resurrect it.
        var userId = Guid.NewGuid();
        var presented = new RefreshToken(userId, "old_hash", DateTimeOffset.UtcNow.AddDays(30));
        var successor = presented.Rotate("new_hash", DateTimeOffset.UtcNow.AddDays(30));
        successor.Revoke();

        _refreshTokenRepo.GetByHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(presented);
        _refreshTokenRepo.GetByIdAsync(successor.Id, Arg.Any<CancellationToken>()).Returns(successor);

        var handler = new RefreshTokenCommandHandler(_userRepo, _refreshTokenRepo, _jwtService);
        var result = await handler.Handle(
            new RefreshTokenCommand("just_rotated"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthErrorCodes.RefreshTokenDead, result.ErrorMessage);
    }

    // ---- ForgotPassword ----

    private readonly IPasswordResetTokenRepository _resetTokenRepo =
        Substitute.For<IPasswordResetTokenRepository>();

    private readonly IEmailVerificationTokenRepository _emailVerificationTokenRepo =
        Substitute.For<IEmailVerificationTokenRepository>();

    [Fact]
    public async Task ForgotPassword_UnknownEmail_ReturnsOkWithoutEnqueueingEmail()
    {
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var handler = new ForgotPasswordCommandHandler(_userRepo, _resetTokenRepo, _backgroundJobs);
        var result = await handler.Handle(
            new ForgotPasswordCommand("missing@example.com"),
            CancellationToken.None);

        // Anti-enumeration: unknown email must be indistinguishable from a known one.
        Assert.True(result.IsSuccess);
        await _resetTokenRepo.DidNotReceive()
            .AddAsync(Arg.Any<PasswordResetToken>(), Arg.Any<CancellationToken>());
        _backgroundJobs.DidNotReceive()
            .EnqueuePasswordResetEmail(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task ForgotPassword_SuspendedUser_ReturnsOkWithoutEnqueueingEmail()
    {
        var user = new User("user@example.com", "hash", status: User.Statuses.Suspended);
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);

        var handler = new ForgotPasswordCommandHandler(_userRepo, _resetTokenRepo, _backgroundJobs);
        var result = await handler.Handle(
            new ForgotPasswordCommand("user@example.com"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        _backgroundJobs.DidNotReceive()
            .EnqueuePasswordResetEmail(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task ForgotPassword_ActiveUser_InvalidatesOldTokensAndEnqueuesEmail()
    {
        var user = new User("user@example.com", "hash");
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);

        var handler = new ForgotPasswordCommandHandler(_userRepo, _resetTokenRepo, _backgroundJobs);
        var result = await handler.Handle(
            new ForgotPasswordCommand("user@example.com"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _resetTokenRepo.Received(1)
            .InvalidateActiveForUserAsync(user.Id, Arg.Any<CancellationToken>());
        await _resetTokenRepo.Received(1)
            .AddAsync(Arg.Is<PasswordResetToken>(t => t.UserId == user.Id && t.IsActive),
                Arg.Any<CancellationToken>());
        _backgroundJobs.Received(1)
            .EnqueuePasswordResetEmail("user@example.com", Arg.Is<string>(t => t.Length == 64));
    }

    // ---- ResetPassword ----

    [Fact]
    public async Task ResetPassword_UnknownToken_ReturnsGenericFailure()
    {
        _resetTokenRepo.GetByHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((PasswordResetToken?)null);

        var handler = new ResetPasswordCommandHandler(
            _userRepo, _resetTokenRepo, _refreshTokenRepo, _emailVerificationTokenRepo, _passwordHasher);
        var result = await handler.Handle(
            new ResetPasswordCommand("unknowntoken", "newpassword1"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid or expired token.", result.ErrorMessage);
    }

    [Fact]
    public async Task ResetPassword_ExpiredToken_ReturnsGenericFailure()
    {
        var token = new PasswordResetToken(Guid.NewGuid(), "hash", DateTimeOffset.UtcNow.AddHours(-1));
        _resetTokenRepo.GetByHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(token);

        var handler = new ResetPasswordCommandHandler(
            _userRepo, _resetTokenRepo, _refreshTokenRepo, _emailVerificationTokenRepo, _passwordHasher);
        var result = await handler.Handle(
            new ResetPasswordCommand("expiredtoken", "newpassword1"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid or expired token.", result.ErrorMessage);
    }

    [Fact]
    public async Task ResetPassword_UsedToken_ReturnsGenericFailure()
    {
        var token = new PasswordResetToken(Guid.NewGuid(), "hash", DateTimeOffset.UtcNow.AddHours(1));
        token.MarkUsed();
        _resetTokenRepo.GetByHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(token);

        var handler = new ResetPasswordCommandHandler(
            _userRepo, _resetTokenRepo, _refreshTokenRepo, _emailVerificationTokenRepo, _passwordHasher);
        var result = await handler.Handle(
            new ResetPasswordCommand("usedtoken", "newpassword1"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid or expired token.", result.ErrorMessage);
    }

    [Fact]
    public async Task ResetPassword_ValidToken_ChangesPasswordMarksUsedAndRevokesSessions()
    {
        var user = new User("user@example.com", "oldhash");
        var token = new PasswordResetToken(user.Id, "hash", DateTimeOffset.UtcNow.AddHours(1));

        _resetTokenRepo.GetByHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(token);
        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Hash("newpassword1").Returns("newhash");

        var handler = new ResetPasswordCommandHandler(
            _userRepo, _resetTokenRepo, _refreshTokenRepo, _emailVerificationTokenRepo, _passwordHasher);
        var result = await handler.Handle(
            new ResetPasswordCommand("validtoken", "newpassword1"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("newhash", user.PasswordHash);
        Assert.NotNull(token.UsedAt);
        await _refreshTokenRepo.Received(1)
            .RevokeAllForUserAsync(user.Id, Arg.Any<CancellationToken>());
    }
}
