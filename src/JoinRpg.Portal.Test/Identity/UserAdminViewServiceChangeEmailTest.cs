using Joinrpg.Web.Identity;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Web.AdminTools;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace JoinRpg.Portal.Test.Identity;

/// <summary>
/// Смена email в админке (#5891): email — это имя пользователя в Identity, и отказ валидатора
/// должен доходить до администратора понятным текстом, а не падать 500.
/// </summary>
public class UserAdminViewServiceChangeEmailTest
{
    private const string OtherUserEmail = "other@example.com";

    [Fact]
    public async Task ValidEmail_IsSaved()
    {
        var (service, store) = Create();

        await service.ChangeEmail(new UserIdentification(store.User.Id), "new+tag@example.com");

        store.User.UserName.ShouldBe("new+tag@example.com");
        store.UpdateCount.ShouldBe(1);
    }

    [Fact]
    public async Task LeadingAndTrailingSpaces_AreTrimmed()
    {
        var (service, store) = Create();

        await service.ChangeEmail(new UserIdentification(store.User.Id), " new+tag@example.com ");

        store.User.UserName.ShouldBe("new+tag@example.com");
        store.UpdateCount.ShouldBe(1);
    }

    [Fact]
    public async Task OnlySpaces_AreRefusedAsEmpty()
    {
        var (service, store) = Create();

        var exception = await Should.ThrowAsync<UserAdminActionRefusedException>(
            service.ChangeEmail(new UserIdentification(store.User.Id), "   "));

        exception.Message.ShouldBe("Email не указан");
        store.UpdateCount.ShouldBe(0);
    }

    [Fact]
    public async Task InvalidCharacters_AreRefusedWithReadableMessage()
    {
        var (service, store) = Create();

        var exception = await Should.ThrowAsync<UserAdminActionRefusedException>(
            service.ChangeEmail(new UserIdentification(store.User.Id), "игрок@example.com"));

        exception.Message.ShouldBe("Недопустимый email: можно использовать латинские буквы, цифры и символы - . _ @ +");
        store.UpdateCount.ShouldBe(0);
    }

    [Fact]
    public async Task EmailOfAnotherUser_IsRefusedWithReadableMessage()
    {
        var (service, store) = Create();

        var exception = await Should.ThrowAsync<UserAdminActionRefusedException>(
            service.ChangeEmail(new UserIdentification(store.User.Id), OtherUserEmail));

        exception.Message.ShouldBe("Этот email уже используется другим пользователем");
    }

    private static (UserAdminViewService, FakeEmailStore) Create()
    {
        var store = new FakeEmailStore();
        var options = new IdentityOptions();
        options.Tokens.ChangeEmailTokenProvider = nameof(AcceptingTokenProvider);
        var manager = new JoinUserManager(
            store,
            Options.Create(options),
            new PasswordHasher<JoinIdentityUser>(),
            [new UserValidator<JoinIdentityUser>()],
            [],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            services: null!,
            NullLogger<JoinUserManager>.Instance,
            new CaptureAccountEmailService());
        manager.RegisterTokenProvider(nameof(AcceptingTokenProvider), new AcceptingTokenProvider());

        // Смене email остальные зависимости не нужны
        var service = new UserAdminViewService(manager, userService: null!, userRepository: null!, externalLoginProfileExtractor: null!);
        return (service, store);
    }

    private sealed class AcceptingTokenProvider : IUserTwoFactorTokenProvider<JoinIdentityUser>
    {
        public Task<string> GenerateAsync(string purpose, UserManager<JoinIdentityUser> manager, JoinIdentityUser user) => Task.FromResult("token");

        public Task<bool> ValidateAsync(string purpose, string token, UserManager<JoinIdentityUser> manager, JoinIdentityUser user) => Task.FromResult(true);

        public Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<JoinIdentityUser> manager, JoinIdentityUser user) => Task.FromResult(false);
    }

    /// <summary>
    /// Как и настоящий <c>MyUserStore</c>, хранит email в имени пользователя.
    /// </summary>
    private sealed class FakeEmailStore : IUserEmailStore<JoinIdentityUser>
    {
        public JoinIdentityUser User { get; } = new JoinIdentityUser() { UserName = "player@example.com", Id = 42 };

        /// <summary>
        /// Сколько раз пользователя сохранили. Identity меняет email на объекте до валидации,
        /// так что неуспех виден только по тому, что сохранения не было.
        /// </summary>
        public int UpdateCount { get; private set; }

        private readonly JoinIdentityUser otherUser = new() { UserName = OtherUserEmail, Id = 43 };

        public Task SetEmailAsync(JoinIdentityUser user, string? email, CancellationToken ct)
        {
            user.UserName = email!;
            return Task.CompletedTask;
        }

        public Task<string?> GetEmailAsync(JoinIdentityUser user, CancellationToken ct) => Task.FromResult<string?>(user.UserName);

        public Task<bool> GetEmailConfirmedAsync(JoinIdentityUser user, CancellationToken ct) => Task.FromResult(user.EmaiLConfirmed);

        public Task SetEmailConfirmedAsync(JoinIdentityUser user, bool confirmed, CancellationToken ct)
        {
            user.EmaiLConfirmed = confirmed;
            return Task.CompletedTask;
        }

        public Task<JoinIdentityUser?> FindByEmailAsync(string normalizedEmail, CancellationToken ct) => FindByNameAsync(normalizedEmail, ct);

        public Task<string?> GetNormalizedEmailAsync(JoinIdentityUser user, CancellationToken ct) => Task.FromResult<string?>(user.UserName.ToUpperInvariant());

        public Task SetNormalizedEmailAsync(JoinIdentityUser user, string? normalizedEmail, CancellationToken ct) => Task.CompletedTask;

        public Task<IdentityResult> UpdateAsync(JoinIdentityUser user, CancellationToken ct)
        {
            UpdateCount++;
            return Task.FromResult(IdentityResult.Success);
        }

        public Task<string> GetUserIdAsync(JoinIdentityUser user, CancellationToken ct) => Task.FromResult(user.Id.ToString());

        public Task<string?> GetUserNameAsync(JoinIdentityUser user, CancellationToken ct) => Task.FromResult<string?>(user.UserName);

        public Task<string?> GetNormalizedUserNameAsync(JoinIdentityUser user, CancellationToken ct) => Task.FromResult<string?>(user.UserName.ToUpperInvariant());

        public Task SetUserNameAsync(JoinIdentityUser user, string? userName, CancellationToken ct) => Task.CompletedTask;

        public Task SetNormalizedUserNameAsync(JoinIdentityUser user, string? normalizedName, CancellationToken ct) => Task.CompletedTask;

        public Task<JoinIdentityUser?> FindByIdAsync(string userId, CancellationToken ct) => Task.FromResult<JoinIdentityUser?>(User);

        // Другого пользователя ищем первым: у объекта User email уже подменён на проверяемый,
        // а настоящая база вернула бы владельца из сохранённых данных.
        public Task<JoinIdentityUser?> FindByNameAsync(string normalizedUserName, CancellationToken ct)
            => Task.FromResult(new[] { otherUser, User }.FirstOrDefault(u => string.Equals(u.UserName, normalizedUserName, StringComparison.OrdinalIgnoreCase)));

        public Task<IdentityResult> CreateAsync(JoinIdentityUser user, CancellationToken ct) => throw new NotSupportedException();

        public Task<IdentityResult> DeleteAsync(JoinIdentityUser user, CancellationToken ct) => throw new NotSupportedException();

        public void Dispose() { }
    }
}
