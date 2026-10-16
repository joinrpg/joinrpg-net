using Joinrpg.Web.Identity;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Portal.Infrastructure.Authentication.Avatars;
using JoinRpg.Services.Interfaces.Avatars;
using JoinRpg.Web.UserProfile;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Portal.Test.Infrastructure;

/// <summary>
/// Несколько карточек профиля в одном дереве рендера просят аватарки одновременно,
/// а EF6-контекст запроса не переносит параллельных операций.
/// </summary>
public class UserAvatarSourceTest(IntegrationTestPortalFactory factory) : IClassFixture<IntegrationTestPortalFactory>
{
    [Fact]
    public async Task ConcurrentRequests_GoOneAtATime()
    {
        var service = new SlowAvatarService();
        var source = new UserAvatarSource(new FakeAvatarLoader(), service);

        await Task.WhenAll(
            source.GetAvatar(new UserIdentification(1), selectedAvatarId: null, 64),
            source.GetAvatar(new UserIdentification(2), selectedAvatarId: null, 64),
            source.GetAvatar(new UserIdentification(3), selectedAvatarId: null, 64));

        service.Calls.ShouldBe(3);
        service.MaxInFlight.ShouldBe(1);
    }

    [Fact]
    public void IsSharedWithinRequest()
    {
        // Очередь работает, только если все карточки запроса получают один и тот же экземпляр.
        using var scope = factory.Services.CreateScope();
        var first = scope.ServiceProvider.GetRequiredService<IUserAvatarSource>();
        var second = scope.ServiceProvider.GetRequiredService<IUserAvatarSource>();

        second.ShouldBeSameAs(first);
    }

    private class SlowAvatarService : IAvatarService
    {
        private int inFlight;

        public int Calls { get; private set; }
        public int MaxInFlight { get; private set; }

        public async Task<AvatarIdentification> EnsureAvatarPresent(int userId)
        {
            var now = Interlocked.Increment(ref inFlight);
            MaxInFlight = Math.Max(MaxInFlight, now);
            Calls++;
            await Task.Delay(20);
            _ = Interlocked.Decrement(ref inFlight);
            return new AvatarIdentification(userId);
        }

        public Task DeleteAvatar(int userId, AvatarIdentification avatarIdentification) => throw new NotImplementedException();
        public Task RecacheAvatar(UserIdentification userId, AvatarIdentification avatarIdentification, CancellationToken ct = default) => throw new NotImplementedException();
        public Task SelectAvatar(int userId, AvatarIdentification avatarIdentification) => throw new NotImplementedException();
    }

    private class FakeAvatarLoader : IAvatarLoader
    {
        public Task<AvatarInfo> GetAvatar(AvatarIdentification userAvatarId, int recommendedSize)
            => Task.FromResult(new AvatarInfo(new Uri($"https://example.com/{userAvatarId.Value}.png"), recommendedSize));
    }
}
