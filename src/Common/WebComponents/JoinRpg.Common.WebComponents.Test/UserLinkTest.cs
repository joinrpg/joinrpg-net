using JoinRpg.Common.PrimitiveTypes;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Common.WebComponents.Test;

public class UserLinkTest : IDisposable
{
    private static readonly string UserIconName = JoinIconDefinitions.Get(JoinIconType.User).IconName;

    private readonly BunitContext _ctx = new();
    private readonly FakeUserLinkLocator _locator = new();

    public UserLinkTest() => _ctx.Services.AddSingleton<IUriLocator<UserLinkViewModel>>(_locator);

    public void Dispose() => _ctx.Dispose();

    private IRenderedComponent<UserLink> Render(UserLinkViewModel model)
        => _ctx.Render<UserLink>(p => p.Add(x => x.Model, model));

    [Fact]
    public void Show_RendersLinkToProfileWithName()
    {
        var cut = Render(new UserLinkViewModel(new UserIdentification(42), "Вася", ViewMode.Show));

        var link = cut.Find("a");
        link.GetAttribute("href").ShouldBe("https://example.com/user/42");
        link.ClassList.ShouldContain("join-user");
        link.TextContent.ShouldContain("Вася");
        cut.Markup.ShouldContain(UserIconName);
    }

    [Fact]
    public void Show_HasNoPrivateHighlight()
    {
        var cut = Render(new UserLinkViewModel(new UserIdentification(42), "Вася", ViewMode.Show));

        cut.Find("a").ClassList.ShouldNotContain("world-object-hidden");
    }

    /// <summary>
    /// Приватность подсвечивается на самой ссылке (см. CharacterLink/CharacterGroupLink),
    /// а не на строке, в которую ссылка вставлена.
    /// </summary>
    [Fact]
    public void ShowAsPrivate_RendersLinkWithPrivateHighlight()
    {
        var cut = Render(new UserLinkViewModel(new UserIdentification(42), "Вася", ViewMode.ShowAsPrivate));

        var link = cut.Find("a");
        link.GetAttribute("href").ShouldBe("https://example.com/user/42");
        link.ClassList.ShouldContain("join-user");
        link.ClassList.ShouldContain("world-object-hidden");
        link.TextContent.ShouldContain("Вася");
    }

    [Fact]
    public void Hide_RendersZanyatoWithoutLink()
    {
        var cut = Render(UserLinkViewModel.Hidden);

        cut.FindAll("a").ShouldBeEmpty();
        cut.Markup.ShouldContain("занято");
        cut.Markup.ShouldContain(UserIconName);
    }

    [Fact]
    public void Hide_DoesNotLeakUserName()
    {
        var cut = Render(new UserLinkViewModel(new UserIdentification(42), "Вася", ViewMode.Hide));

        cut.Markup.ShouldNotContain("Вася");
    }

    /// <summary>
    /// У скрытого пользователя ссылки на профиль нет, поэтому реальные локаторы
    /// (и в Portal, и в Blazor-клиенте) на запрос url в режиме Hide кидают исключение.
    /// Компонент обязан до них не доходить.
    /// </summary>
    [Fact]
    public void Hide_DoesNotAskLocatorForUrl()
    {
        var cut = Render(UserLinkViewModel.Hidden);

        cut.Markup.ShouldContain("занято");
        _locator.Calls.ShouldBeEmpty();
    }

    private sealed class FakeUserLinkLocator : IUriLocator<UserLinkViewModel>
    {
        public List<UserLinkViewModel> Calls { get; } = [];

        public Uri GetUri(UserLinkViewModel target)
        {
            if (target.ViewMode == ViewMode.Hide)
            {
                throw new InvalidOperationException("Should not have url of hidden");
            }
            Calls.Add(target);
            return new Uri($"https://example.com/user/{target.UserId.Value}");
        }
    }
}
