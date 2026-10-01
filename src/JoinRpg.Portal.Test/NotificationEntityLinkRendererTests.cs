using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.DomainTypes.Claims;
using JoinRpg.Interfaces.Notifications;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Portal.Test;

public class NotificationEntityLinkRendererTests(IntegrationTestPortalFactory factory)
    : IClassFixture<IntegrationTestPortalFactory>
{
    private INotificationEntityLinkRenderer Resolve(IServiceScope scope)
        => scope.ServiceProvider.GetRequiredService<INotificationEntityLinkRenderer>();

    [Fact]
    public void RendersLinkForClaimComment()
    {
        using var scope = factory.Services.CreateScope();
        var renderer = Resolve(scope);

        var link = renderer.RenderEntityLink(new ClaimCommentIdentification(new ClaimIdentification(1, 1), 42));

        link.ShouldNotBeNull();
        link.Markdown.Value.ShouldNotBeNull();
        link.Markdown.Value.ShouldStartWith("Подробнее: [комментарий](http");
        link.Markdown.Value.ShouldContain("#comment42");
        link.PlainText.ShouldStartWith("Подробнее: комментарий: http");
        link.PlainText.ShouldContain("#comment42");
    }

    [Fact]
    public void RendersLinkForProject()
    {
        using var scope = factory.Services.CreateScope();
        var renderer = Resolve(scope);

        var link = renderer.RenderEntityLink(new ProjectIdentification(1));

        link.ShouldNotBeNull();
        link.Markdown.Value.ShouldStartWith("Подробнее: [проект](http");
        link.PlainText.ShouldStartWith("Подробнее: проект: http");
    }

    [Fact]
    public void RendersLinkForAccommodationRoom()
    {
        using var scope = factory.Services.CreateScope();
        var renderer = Resolve(scope);

        // Отдельной страницы комнаты нет, поэтому ссылка ведёт на список комнат проекта.
        var link = renderer.RenderEntityLink(new AccommodationRoomIdentification(new ProjectIdentification(1), 7));

        link.ShouldNotBeNull();
        link.Markdown.Value.ShouldStartWith("Подробнее: [комнаты](http");
        link.PlainText.ShouldStartWith("Подробнее: комнаты: http");
    }

    [Fact]
    public void ReturnsNullForNullReference()
    {
        using var scope = factory.Services.CreateScope();
        var renderer = Resolve(scope);

        renderer.RenderEntityLink(null).ShouldBeNull();
    }
}
