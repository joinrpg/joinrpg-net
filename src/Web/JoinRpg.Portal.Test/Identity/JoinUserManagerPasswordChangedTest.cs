using Joinrpg.Web.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace JoinRpg.Portal.Test.Identity;

/// <summary>
/// Письмо «ваш пароль был изменён» (#1092): уходит при любой успешной смене пароля
/// и не уходит, если пароль не поменялся.
/// </summary>
public class JoinUserManagerPasswordChangedTest
{
    private const string OldPassword = "OldPassword123!";
    private const string NewPassword = "NewPassword456!";

    [Fact]
    public async Task ChangePassword_SendsEmail()
    {
        var (manager, emails, store) = Create();
        _ = await manager.AddPasswordAsync(store.User, OldPassword);
        emails.PasswordChanged.Clear();

        var result = await manager.ChangePasswordAsync(store.User, OldPassword, NewPassword);

        result.Succeeded.ShouldBeTrue();
        emails.PasswordChanged.ShouldBe([store.User]);
    }

    [Fact]
    public async Task ChangePassword_WithWrongCurrentPassword_DoesNotSendEmail()
    {
        var (manager, emails, store) = Create();
        _ = await manager.AddPasswordAsync(store.User, OldPassword);
        emails.PasswordChanged.Clear();

        var result = await manager.ChangePasswordAsync(store.User, "wrong", NewPassword);

        result.Succeeded.ShouldBeFalse();
        emails.PasswordChanged.ShouldBeEmpty();
    }

    [Fact]
    public async Task AddPassword_SendsEmail()
    {
        var (manager, emails, store) = Create();

        var result = await manager.AddPasswordAsync(store.User, NewPassword);

        result.Succeeded.ShouldBeTrue();
        emails.PasswordChanged.ShouldBe([store.User]);
    }

    [Fact]
    public async Task AddPassword_WhenPasswordAlreadySet_DoesNotSendEmail()
    {
        var (manager, emails, store) = Create();
        _ = await manager.AddPasswordAsync(store.User, OldPassword);
        emails.PasswordChanged.Clear();

        var result = await manager.AddPasswordAsync(store.User, NewPassword);

        result.Succeeded.ShouldBeFalse();
        emails.PasswordChanged.ShouldBeEmpty();
    }

    [Fact]
    public async Task ResetPassword_WithInvalidToken_DoesNotSendEmail()
    {
        var (manager, emails, store) = Create();

        var result = await manager.ResetPasswordAsync(store.User, "invalid-token", NewPassword);

        result.Succeeded.ShouldBeFalse();
        emails.PasswordChanged.ShouldBeEmpty();
    }

    private static (JoinUserManager, CaptureAccountEmailService, FakePasswordStore) Create()
    {
        var store = new FakePasswordStore();
        var emails = new CaptureAccountEmailService();
        var options = new IdentityOptions();
        options.Tokens.PasswordResetTokenProvider = nameof(RejectingTokenProvider);
        var manager = new JoinUserManager(
            store,
            Options.Create(options),
            new PasswordHasher<JoinIdentityUser>(),
            [],
            [],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            services: null!,
            NullLogger<JoinUserManager>.Instance,
            emails);
        manager.RegisterTokenProvider(nameof(RejectingTokenProvider), new RejectingTokenProvider());
        return (manager, emails, store);
    }

    private sealed class RejectingTokenProvider : IUserTwoFactorTokenProvider<JoinIdentityUser>
    {
        public Task<string> GenerateAsync(string purpose, UserManager<JoinIdentityUser> manager, JoinIdentityUser user) => throw new NotSupportedException();

        public Task<bool> ValidateAsync(string purpose, string token, UserManager<JoinIdentityUser> manager, JoinIdentityUser user) => Task.FromResult(false);

        public Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<JoinIdentityUser> manager, JoinIdentityUser user) => Task.FromResult(false);
    }

    private sealed class FakePasswordStore : IUserPasswordStore<JoinIdentityUser>
    {
        public JoinIdentityUser User { get; } = new JoinIdentityUser() { UserName = "player@example.com", Id = 42 };

        public Task SetPasswordHashAsync(JoinIdentityUser user, string? passwordHash, CancellationToken ct)
        {
            user.PasswordHash = passwordHash;
            return Task.CompletedTask;
        }

        public Task<string?> GetPasswordHashAsync(JoinIdentityUser user, CancellationToken ct) => Task.FromResult(user.PasswordHash);

        public Task<bool> HasPasswordAsync(JoinIdentityUser user, CancellationToken ct) => Task.FromResult(user.PasswordHash is not null);

        public Task<IdentityResult> UpdateAsync(JoinIdentityUser user, CancellationToken ct) => Task.FromResult(IdentityResult.Success);

        public Task<string> GetUserIdAsync(JoinIdentityUser user, CancellationToken ct) => Task.FromResult(user.Id.ToString());

        public Task<string?> GetUserNameAsync(JoinIdentityUser user, CancellationToken ct) => Task.FromResult<string?>(user.UserName);

        public Task<string?> GetNormalizedUserNameAsync(JoinIdentityUser user, CancellationToken ct) => Task.FromResult<string?>(user.UserName.ToUpperInvariant());

        public Task SetUserNameAsync(JoinIdentityUser user, string? userName, CancellationToken ct) => Task.CompletedTask;

        public Task SetNormalizedUserNameAsync(JoinIdentityUser user, string? normalizedName, CancellationToken ct) => Task.CompletedTask;

        public Task<JoinIdentityUser?> FindByIdAsync(string userId, CancellationToken ct) => Task.FromResult<JoinIdentityUser?>(User);

        public Task<JoinIdentityUser?> FindByNameAsync(string normalizedUserName, CancellationToken ct) => Task.FromResult<JoinIdentityUser?>(User);

        public Task<IdentityResult> CreateAsync(JoinIdentityUser user, CancellationToken ct) => throw new NotSupportedException();

        public Task<IdentityResult> DeleteAsync(JoinIdentityUser user, CancellationToken ct) => throw new NotSupportedException();

        public void Dispose() { }
    }
}
