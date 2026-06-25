using Lexify.Application.Abstractions;
using Lexify.Application.Auth.Commands.Login;
using Lexify.Application.Auth.Commands.RefreshToken;
using Lexify.Application.Auth.Commands.Register;
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

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsFailure()
    {
        var existingUser = new User("user@example.com", "hash");
        _userRepo.GetByEmailAsync("user@example.com", Arg.Any<CancellationToken>())
            .Returns(existingUser);

        var handler = new RegisterCommandHandler(_userRepo, _passwordHasher);
        var result = await handler.Handle(
            new RegisterCommand("user@example.com", "password", null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Failure, result.Status);
    }

    [Fact]
    public async Task Register_NewEmail_ReturnsOkWithGuid()
    {
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);
        _passwordHasher.Hash("password").Returns("hashed");

        var handler = new RegisterCommandHandler(_userRepo, _passwordHasher);
        var result = await handler.Handle(
            new RegisterCommand("new@example.com", "password", "Alice"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
    }

    // ---- Login ----

    private readonly IRefreshTokenRepository _refreshTokenRepo =
        Substitute.For<IRefreshTokenRepository>();
    private readonly IJwtService _jwtService = Substitute.For<IJwtService>();

    [Fact]
    public async Task Login_UserNotFound_ReturnsFailure()
    {
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var handler = new LoginCommandHandler(_userRepo, _refreshTokenRepo, _passwordHasher, _jwtService);
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

        var handler = new LoginCommandHandler(_userRepo, _refreshTokenRepo, _passwordHasher, _jwtService);
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

        var handler = new LoginCommandHandler(_userRepo, _refreshTokenRepo, _passwordHasher, _jwtService);
        var result = await handler.Handle(
            new LoginCommand("user@example.com", "pass"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Account is not active.", result.ErrorMessage);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkWithTokens()
    {
        var user = new User("user@example.com", "hash");
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _jwtService.GenerateAccessToken(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns("access_token");
        _jwtService.GetExpiry().Returns(DateTimeOffset.UtcNow.AddHours(1));

        var handler = new LoginCommandHandler(_userRepo, _refreshTokenRepo, _passwordHasher, _jwtService);
        var result = await handler.Handle(
            new LoginCommand("user@example.com", "pass"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("access_token", result.Value!.AccessToken);
        Assert.NotNull(result.Value.RefreshToken);
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
        Assert.Equal("Invalid or expired refresh token.", result.ErrorMessage);
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
        Assert.Equal("Invalid or expired refresh token.", result.ErrorMessage);
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
        Assert.Equal("Invalid or expired refresh token.", result.ErrorMessage);
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
}
