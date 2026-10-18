using Bunit;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes.Users;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.UserProfile.Test;

public class UserProfilePanelTest : IDisposable
{
    private readonly BunitContext ctx = new();

    public UserProfilePanelTest()
        => _ = ctx.Services.AddSingleton<IUriLocator<UserLinkViewModel>>(new FakeUserLinkLocator());

    public void Dispose() => ctx.Dispose();

    private IRenderedComponent<UserProfilePanel> Render(UserProfileDetailsViewModel model)
        => ctx.Render<UserProfilePanel>(p => p.Add(x => x.Model, model));

    [Fact]
    public void ShowsAvatarPreparedByServer()
    {
        var model = new UserProfileDetailsViewModel(Player(), currentUser: null)
        {
            Avatar = new AvatarInfo(new Uri("https://example.com/avatar.png"), 64),
        };

        Render(model).Find("img").GetAttribute("src").ShouldBe("https://example.com/avatar.png");
    }

    [Fact]
    public void WithoutPreparedAvatar_RendersNoImage()
        => Render(new UserProfileDetailsViewModel(Player(), currentUser: null)).FindAll("img").ShouldBeEmpty();

    [Fact]
    public void OwnProfile_FooterLinksToProfileSetup()
    {
        var cut = Render(new UserProfileDetailsViewModel(Player(), Player()));

        var footer = cut.Find(".panel-footer");
        footer.TextContent.ShouldContain("Это мой профиль");
        footer.QuerySelector("a")!.GetAttribute("href").ShouldBe("/manage/setupprofile");
    }

    /// <summary>Blazor вырезает пробелы между текстом и разметкой; без @preservewhitespace получится «профиль[».</summary>
    [Fact]
    public void OwnProfile_FooterKeepsSpaceBeforeSetupLink()
    {
        var cut = Render(new UserProfileDetailsViewModel(Player(), Player()));

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
        cut.Markup.ShouldContain("Пользователь предпочитает сохранять анонимность.");
    }

    [Fact]
    public void Admin_SeesContactsAndMissingPassword_WithoutSetupLink()
    {
        var admin = Player() with { UserId = new UserIdentification(1), IsAdmin = true };

        var cut = Render(new UserProfileDetailsViewModel(Player(), admin));

        cut.Markup.ShouldContain("player@example.com");
        cut.Markup.ShouldContain("Нет пароля");
        var footer = cut.Find(".panel-footer");
        footer.TextContent.ShouldContain("администратор");
        footer.QuerySelector("a").ShouldBeNull();
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

    private class FakeUserLinkLocator : IUriLocator<UserLinkViewModel>
    {
        public Uri GetUri(UserLinkViewModel target) => new($"https://example.com/user/{target.UserId?.Value}");
    }
}
