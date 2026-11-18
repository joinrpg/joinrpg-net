using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DomainTypes.Users;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.UserProfile.Test;

public class UserProfileDetailsBuilderTest
{
    private readonly UserProfileDetailsBuilder builder = new(new FakeAvatarUriResolver());

    [Fact]
    public void SelectedAvatar_IsResolvedIntoCard()
    {
        var avatar = new UserAvatarSource(new AvatarIdentification(7), CachedUri: "avatars/7.png", OriginalUri: "https://vk.example.com/7.png");

        var card = builder.Build(User(avatar), currentUser: null);

        card.Avatar.ShouldBe(new AvatarInfo(new Uri("https://cache.example.com/avatars/7.png"), 64));
    }

    [Fact]
    public void NoSelectedAvatar_CardWithoutAvatar()
        => builder.Build(User(avatar: null), currentUser: null).Avatar.ShouldBeNull();

    [Fact]
    public void AvatarIsShown_EvenWithoutAccessToContacts()
    {
        // Аватарка — как имя: её видят все, кому видна карточка, а не только те, кому видны контакты.
        var avatar = new UserAvatarSource(new AvatarIdentification(7), CachedUri: null, OriginalUri: "https://www.gravatar.com/avatar/x");
        var stranger = User(avatar: null) with { UserId = new UserIdentification(100500) };

        var card = builder.Build(User(avatar), stranger);

        card.Reason.ShouldBe(AccessReasonView.NoAccess);
        card.Avatar.ShouldNotBeNull().Uri.ShouldBe(new Uri("https://www.gravatar.com/avatar/x"));
    }

    private static UserInfo User(UserAvatarSource? avatar) => new(
        new UserIdentification(42),
        new UserSocialNetworks(null, null, null, null, ContactsAccessType.OnlyForMasters),
        ActiveClaims: [],
        ActiveProjects: [],
        AllProjects: [],
        IsAdmin: false,
        SelectedAvatar: avatar,
        new Email("player@example.com"),
        EmailConfirmed: true,
        new UserFullName(new PrefferedName("Вася"), null, null, null),
        VerifiedProfileFlag: false,
        PhoneNumber: null,
        HasPassword: false);

    private class FakeAvatarUriResolver : IAvatarUriResolver
    {
        public Uri GetAvatarUri(string? cachedUri, string originalUri)
            => cachedUri is null ? new Uri(originalUri) : new Uri($"https://cache.example.com/{cachedUri}");
    }
}
