using Joinrpg.Web.Identity;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Users;
using JoinRpg.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace JoinRpg.Portal.Test.Identity;

/// <summary>
/// Отвязка соцсети с профиля (#3575).
/// </summary>
/// <remarks>
/// Неподтверждённая соцсеть может не иметь записи в <c>AspNetUserLogins</c> — она лежит только
/// в legacy-полях профиля (<c>UserExtra.Vk</c>/<c>UserExtra.Telegram</c>). По данным профиля
/// отличить такую запись от настоящей привязки нельзя: legacy-ВК хранит числовой id и выглядит
/// ровно как привязанный. Раньше форма отвязки присылала этот id как providerKey, и удаление
/// падало в <c>MyUserStore.RemoveLoginAsync</c> (там <c>First()</c> по несуществующей записи) —
/// пользователь не мог убрать неподтверждённый ВК вообще.
/// </remarks>
public class ExternalLoginProfileExtractorRemoveLoginTest
{
    [Fact]
    public async Task VkWithoutExternalLogin_CleansProfileAndSucceeds()
    {
        var (extractor, userService, store) = Create();

        var result = await extractor.RemoveLogin(store.User, UserExternalLogin.VkProvider);

        result.Succeeded.ShouldBeTrue();
        userService.RemovedVk.ShouldBe(new UserIdentification(store.User.Id));
        store.RemovedLogins.ShouldBeEmpty();
    }

    [Fact]
    public async Task VkWithExternalLogin_RemovesLoginAndCleansProfile()
    {
        var (extractor, userService, store) = Create();
        store.Logins.Add(new UserLoginInfo(UserExternalLogin.VkProvider, "123", UserExternalLogin.VkProvider));

        var result = await extractor.RemoveLogin(store.User, UserExternalLogin.VkProvider);

        result.Succeeded.ShouldBeTrue();
        store.RemovedLogins.ShouldBe([(UserExternalLogin.VkProvider, "123")]);
        userService.RemovedVk.ShouldBe(new UserIdentification(store.User.Id));
    }

    [Fact]
    public async Task TelegramWithoutExternalLogin_CleansProfileAndSucceeds()
    {
        var (extractor, userService, store) = Create();

        var result = await extractor.RemoveLogin(store.User, UserExternalLogin.TelegramProvider);

        result.Succeeded.ShouldBeTrue();
        userService.RemovedTelegram.ShouldBe(store.User.Id);
        store.RemovedLogins.ShouldBeEmpty();
    }

    [Fact]
    public async Task OtherProviderLogins_AreNotTouched()
    {
        var (extractor, _, store) = Create();
        store.Logins.Add(new UserLoginInfo(UserExternalLogin.TelegramProvider, "456", UserExternalLogin.TelegramProvider));

        _ = await extractor.RemoveLogin(store.User, UserExternalLogin.VkProvider);

        store.RemovedLogins.ShouldBeEmpty();
    }

    private static (ExternalLoginProfileExtractor, FakeUserService, FakeLoginStore) Create()
    {
        var store = new FakeLoginStore();
        var userManager = new JoinUserManager(
            store,
            Options.Create(new IdentityOptions()),
            new PasswordHasher<JoinIdentityUser>(),
            [],
            [],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            services: null!,
            NullLogger<JoinUserManager>.Instance);
        var userService = new FakeUserService();
        return (
            new ExternalLoginProfileExtractor(userService, userManager, NullLogger<ExternalLoginProfileExtractor>.Instance),
            userService,
            store);
    }

    private sealed class FakeLoginStore : IUserLoginStore<JoinIdentityUser>
    {
        public JoinIdentityUser User { get; } = new JoinIdentityUser() { UserName = "player@example.com", Id = 42 };

        public List<UserLoginInfo> Logins { get; } = [];

        public List<(string Provider, string Key)> RemovedLogins { get; } = [];

        public Task AddLoginAsync(JoinIdentityUser user, UserLoginInfo login, CancellationToken ct)
        {
            Logins.Add(login);
            return Task.CompletedTask;
        }

        public Task RemoveLoginAsync(JoinIdentityUser user, string loginProvider, string providerKey, CancellationToken ct)
        {
            var login = Logins.Single(l => l.LoginProvider == loginProvider && l.ProviderKey == providerKey);
            _ = Logins.Remove(login);
            RemovedLogins.Add((loginProvider, providerKey));
            return Task.CompletedTask;
        }

        public Task<IList<UserLoginInfo>> GetLoginsAsync(JoinIdentityUser user, CancellationToken ct)
            => Task.FromResult<IList<UserLoginInfo>>([.. Logins]);

        public Task<JoinIdentityUser?> FindByLoginAsync(string loginProvider, string providerKey, CancellationToken ct)
            => Task.FromResult<JoinIdentityUser?>(
                Logins.Any(l => l.LoginProvider == loginProvider && l.ProviderKey == providerKey) ? User : null);

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

    private sealed class FakeUserService : IUserService
    {
        public UserIdentification? RemovedVk { get; private set; }

        public int? RemovedTelegram { get; private set; }

        public Task RemoveVkFromProfile(UserIdentification id)
        {
            RemovedVk = id;
            return Task.CompletedTask;
        }

        public Task RemoveTelegramFromProfile(int id)
        {
            RemovedTelegram = id;
            return Task.CompletedTask;
        }

        public Task UpdateProfile(int userId, UserFullName userFullName, Gender gender, string phoneNumber, string nicknames, string groupNames, string livejournal, ContactsAccessType socialNetworkAccess, string passportData, string registrationAddress, DateOnly? birthDate) => throw new NotSupportedException();

        public Task SetAdminFlag(int userId, bool administratorFlag) => throw new NotSupportedException();

        public Task SetVerificationFlag(int userId, bool verificationFlag) => throw new NotSupportedException();

        public Task SetNameIfNotSetWithoutAccessChecks(int userId, UserFullName userFullName) => throw new NotSupportedException();

        public Task SetVkIfNotSetWithoutAccessChecks(int id, VkSocialLink vk, AvatarInfo? avatarInfo, CancellationToken ct = default) => throw new NotSupportedException();

        public Task SetBirthDateIfNotSetWithoutAccessChecks(UserIdentification userId, DateOnly birthDate) => throw new NotSupportedException();

        public Task SetBirthDate(UserIdentification userId, DateOnly? birthDate) => throw new NotSupportedException();

        public Task SetTelegramIfNotSetWithoutAccessChecks(int id, TelegramSocialLink telegram, AvatarInfo? avatarInfo, CancellationToken ct = default) => throw new NotSupportedException();
    }
}
