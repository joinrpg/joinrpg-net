using Joinrpg.Web.Identity;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DomainTypes.Users;
using JoinRpg.Portal.Infrastructure.Authentication.Avatars;
using JoinRpg.Services.Interfaces.Avatars;
using JoinRpg.Web.UserProfile;

namespace JoinRpg.Portal.Test.Infrastructure;

public class UserProfileAvatarsTest
{
    private readonly FakeAvatarService service = new();
    private UserProfileAvatars Avatars => new(new FakeAvatarLoader(), service);

    [Fact]
    public async Task SelectedAvatar_IsUsedWithoutCreatingDefault()
    {
        var card = Card(userId: 1, selectedAvatarId: 77);

        await Avatars.Fill(card);

        card.Avatar.ShouldNotBeNull().Uri.ShouldBe(new Uri("https://example.com/77.png"));
        service.EnsuredFor.ShouldBeEmpty();
    }

    [Fact]
    public async Task NoSelectedAvatar_DefaultIsEnsured()
    {
        var card = Card(userId: 2, selectedAvatarId: null);

        await Avatars.Fill(card);

        service.EnsuredFor.ShouldBe([2]);
        card.Avatar.ShouldNotBeNull().Uri.ShouldBe(new Uri("https://example.com/1002.png"));
    }

    [Fact]
    public async Task FillsEveryCard()
    {
        UserProfileDetailsViewModel[] cards = [Card(1, 11), Card(2, null), Card(3, 33)];

        await Avatars.Fill(cards);

        cards.ShouldAllBe(c => c.Avatar != null);
    }

    private static UserProfileDetailsViewModel Card(int userId, int? selectedAvatarId) => new UserProfileDetailsBuilder().Build(
        new UserInfo(
            new UserIdentification(userId),
            new UserSocialNetworks(null, null, null, null, ContactsAccessType.OnlyForMasters),
            ActiveClaims: [],
            ActiveProjects: [],
            AllProjects: [],
            IsAdmin: false,
            AvatarIdentification.FromOptional(selectedAvatarId),
            new Email($"user{userId}@example.com"),
            EmailConfirmed: true,
            new UserFullName(new PrefferedName($"user{userId}"), null, null, null),
            VerifiedProfileFlag: false,
            PhoneNumber: null,
            HasPassword: true),
        currentUser: null);

    private class FakeAvatarService : IAvatarService
    {
        public List<int> EnsuredFor { get; } = [];

        public Task<AvatarIdentification> EnsureAvatarPresent(int userId)
        {
            EnsuredFor.Add(userId);
            return Task.FromResult(new AvatarIdentification(1000 + userId));
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
