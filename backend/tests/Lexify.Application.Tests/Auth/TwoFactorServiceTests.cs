using Lexify.Application.Abstractions;
using Lexify.Application.Auth.Common;
using Lexify.Domain.Entities;
using Lexify.Domain.Repositories;
using NSubstitute;

namespace Lexify.Application.Tests.Auth;

public class TwoFactorServiceTests
{
    private readonly ILoginTwoFactorCodeRepository _codeRepo =
        Substitute.For<ILoginTwoFactorCodeRepository>();
    private readonly ISystemSettingRepository _settingRepo = Substitute.For<ISystemSettingRepository>();
    private readonly IBackgroundJobService _backgroundJobs = Substitute.For<IBackgroundJobService>();

    private TwoFactorService CreateService() => new(_codeRepo, _settingRepo, _backgroundJobs);

    private void GivenGlobalSwitch(string? value)
    {
        _settingRepo.GetByKeyAsync(SystemSetting.Keys.TwoFactorEnabled, Arg.Any<CancellationToken>())
            .Returns(value is null ? null : new SystemSetting(SystemSetting.Keys.TwoFactorEnabled, value, "bool"));
    }

    // ---- IsGloballyEnabledAsync (fails OPEN) ----

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("not-a-bool", false)]
    public async Task IsGloballyEnabled_ReflectsSetting(string value, bool expected)
    {
        GivenGlobalSwitch(value);
        Assert.Equal(expected, await CreateService().IsGloballyEnabledAsync());
    }

    [Fact]
    public async Task IsGloballyEnabled_MissingRow_FailsOpenToFalse()
    {
        GivenGlobalSwitch(null);
        Assert.False(await CreateService().IsGloballyEnabledAsync());
    }

    // ---- IsRequiredForAsync (policy truth table) ----

    [Fact]
    public async Task IsRequiredFor_GlobalOff_FalseEvenForAdmin()
    {
        GivenGlobalSwitch("false");
        var admin = new User("admin@example.com", "hash", role: User.Roles.Admin);
        Assert.False(await CreateService().IsRequiredForAsync(admin));
    }

    [Fact]
    public async Task IsRequiredFor_GlobalOn_Admin_AlwaysTrue()
    {
        GivenGlobalSwitch("true");
        var admin = new User("admin@example.com", "hash", role: User.Roles.Admin);
        Assert.True(await CreateService().IsRequiredForAsync(admin));
    }

    [Fact]
    public async Task IsRequiredFor_GlobalOn_NonAdminOptedIn_True()
    {
        GivenGlobalSwitch("true");
        var user = new User("user@example.com", "hash");
        user.EnableTwoFactor();
        Assert.True(await CreateService().IsRequiredForAsync(user));
    }

    [Fact]
    public async Task IsRequiredFor_GlobalOn_NonAdminNotOptedIn_False()
    {
        GivenGlobalSwitch("true");
        var user = new User("user@example.com", "hash");
        Assert.False(await CreateService().IsRequiredForAsync(user));
    }

    // ---- IssueCodeAsync ----

    [Fact]
    public async Task IssueCode_SupersedesActive_StoresHash_EmailsMatchingCode()
    {
        var user = new User("user@example.com", "hash");
        LoginTwoFactorCode? stored = null;
        await _codeRepo.AddAsync(Arg.Do<LoginTwoFactorCode>(c => stored = c), Arg.Any<CancellationToken>());
        string? emailed = null;
        _backgroundJobs.When(b => b.EnqueueTwoFactorCode(user.Email, Arg.Any<string>()))
            .Do(ci => emailed = ci.ArgAt<string>(1));

        await CreateService().IssueCodeAsync(user);

        await _codeRepo.Received(1).InvalidateActiveForUserAsync(user.Id, Arg.Any<CancellationToken>());
        await _codeRepo.Received(1).AddAsync(Arg.Any<LoginTwoFactorCode>(), Arg.Any<CancellationToken>());
        Assert.NotNull(stored);
        Assert.NotNull(emailed);
        Assert.Matches("^[0-9]{6}$", emailed!);
        // The emailed code is exactly what was hashed into the stored row — never plaintext in the DB.
        Assert.Equal(ITwoFactorService.HashCode(emailed!), stored!.CodeHash);
    }

    // ---- VerifyCodeAsync ----

    [Fact]
    public async Task VerifyCode_Correct_ConsumesAndReturnsTrue()
    {
        var user = new User("user@example.com", "hash");
        var code = new LoginTwoFactorCode(
            user.Id, ITwoFactorService.HashCode("123456"), DateTimeOffset.UtcNow.AddMinutes(10));
        _codeRepo.GetActiveForUserAsync(user.Id, Arg.Any<CancellationToken>()).Returns(code);

        var ok = await CreateService().VerifyCodeAsync(user, "123456");

        Assert.True(ok);
        await _codeRepo.Received(1).MarkUsedAsync(code.Id, Arg.Any<CancellationToken>());
        await _codeRepo.DidNotReceive().IncrementAttemptsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task VerifyCode_Wrong_IncrementsAttemptsAndReturnsFalse()
    {
        var user = new User("user@example.com", "hash");
        var code = new LoginTwoFactorCode(
            user.Id, ITwoFactorService.HashCode("123456"), DateTimeOffset.UtcNow.AddMinutes(10));
        _codeRepo.GetActiveForUserAsync(user.Id, Arg.Any<CancellationToken>()).Returns(code);

        var ok = await CreateService().VerifyCodeAsync(user, "000000");

        Assert.False(ok);
        await _codeRepo.Received(1).IncrementAttemptsAsync(code.Id, Arg.Any<CancellationToken>());
        await _codeRepo.DidNotReceive().MarkUsedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task VerifyCode_NoActiveCode_ReturnsFalse()
    {
        var user = new User("user@example.com", "hash");
        _codeRepo.GetActiveForUserAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns((LoginTwoFactorCode?)null);

        Assert.False(await CreateService().VerifyCodeAsync(user, "123456"));
    }
}
