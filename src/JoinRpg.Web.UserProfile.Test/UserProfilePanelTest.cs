using Bunit;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes.Users;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.UserProfile.Test;

public class UserProfilePanelTest : IDisposable
{
    private readonly BunitContext ctx = new();
    private readonly FakeAvatarSource avatars = new();

    private static readonly UserInfoHeader user = new(new UserIdentification(42), new UserDisplayName("Вася", null));

    public UserProfilePanelTest()
    {
        _ = ctx.Services.AddSingleton<IUserAvatarSource>(avatars);
        _ = ctx.Services.AddSingleton<IUriLocator<UserLinkViewModel>>(new FakeUserLinkLocator());
    }

    public void Dispose() => ctx.Dispose();

    private IRenderedComponent<UserProfilePanel> Render(UserProfileDetailsViewModel model)
        => ctx.Render<UserProfilePanel>(p => p.Add(x => x.Model, model));

    [Fact]
    public void LoadsAvatarOfShownUser()
    {
        var cut = Render(new UserProfileDetailsViewModel(user, currentUserId: null));

        avatars.RequestedFor.ShouldBe([new UserIdentification(42)]);
        cut.Find("img").GetAttribute("src").ShouldBe(FakeAvatarSource.AvatarUri.AbsoluteUri);
    }

    [Fact]
    public void OwnProfile_FooterLinksToProfileSetup()
    {
        var cut = Render(new UserProfileDetailsViewModel(user, currentUserId: new UserIdentification(42)));

        var footer = cut.Find(".panel-footer");
        footer.TextContent.ShouldContain("Это мой профиль");
        footer.QuerySelector("a")!.GetAttribute("href").ShouldBe("/manage/setupprofile");
    }

    [Fact]
    public void OtherProfile_FooterHasNoSetupLink()
    {
        var cut = Render(new UserProfileDetailsViewModel(user, currentUserId: new UserIdentification(1)));

        cut.Find(".panel-footer").QuerySelector("a").ShouldBeNull();
    }

    /// <summary>Blazor вырезает пробелы между текстом и разметкой; без @preservewhitespace получится «профиль[».</summary>
    [Fact]
    public void OwnProfile_FooterKeepsSpaceBeforeSetupLink()
    {
        var cut = Render(new UserProfileDetailsViewModel(user, currentUserId: new UserIdentification(42)));

        // Браузер схлопывает любые пробельные символы в один пробел — сравниваем так же.
        var text = string.Join(' ', cut.Find(".panel-footer").TextContent.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        text.ShouldBe("Это мой профиль [ Настроить ]");
    }

    [Fact]
    public void Stranger_SeesNoContactsAndNoFooter()
    {
        var cut = Render(new UserProfileDetailsViewModel(Player(), currentUser: null));

        cut.Markup.ShouldNotContain("player@example.com");
        cut.Markup.ShouldNotContain("+79990000000");
        cut.FindAll(".panel-footer").ShouldBeEmpty();
        cut.Markup.ShouldContain("Игрок предпочитает сохранять анонимность.");
    }

    [Fact]
    public void Admin_SeesContactsAndMissingPassword()
    {
        var admin = Player() with { UserId = new UserIdentification(1), IsAdmin = true };

        var cut = Render(new UserProfileDetailsViewModel(Player(), admin));

        cut.Markup.ShouldContain("player@example.com");
        cut.Markup.ShouldContain("Нет пароля");
        cut.Find(".panel-footer").TextContent.ShouldContain("администратор");
    }

    private static UserInfo Player() => new(
        new UserIdentification(42),
        new UserSocialNetworks(null, null, null, null, ContactsAccessType.OnlyForMasters),
        ActiveClaims: [],
        ActiveProjects: [],
        AllProjects: [],
        IsAdmin: false,
        SelectedAvatarId: null,
        new Email("player@example.com"),
        EmailConfirmed: true,
        new UserFullName(new PrefferedName("Вася"), null, null, null),
        VerifiedProfileFlag: false,
        PhoneNumber: "+79990000000",
        HasPassword: false);

    private class FakeAvatarSource : IUserAvatarSource
    {
        public static readonly Uri AvatarUri = new("https://example.com/avatar.png");

        public List<UserIdentification> RequestedFor { get; } = [];

        public Task<AvatarInfo> GetAvatar(UserIdentification userId, AvatarIdentification? selectedAvatarId, int recommendedSize)
        {
            RequestedFor.Add(userId);
            return Task.FromResult(new AvatarInfo(AvatarUri, recommendedSize));
        }
    }

    private class FakeUserLinkLocator : IUriLocator<UserLinkViewModel>
    {
        public Uri GetUri(UserLinkViewModel target) => new($"https://example.com/user/{target.UserId?.Value}");
    }
}
