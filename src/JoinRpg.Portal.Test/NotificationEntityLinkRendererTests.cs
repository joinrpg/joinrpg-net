using JoinRpg.DomainTypes;
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
        link.Markdown.Contents.ShouldNotBeNull();
        link.Markdown.Contents.ShouldStartWith("Подробнее: [комментарий](http");
        link.Markdown.Contents.ShouldContain("#comment42");
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
        link.Markdown.Contents.ShouldStartWith("Подробнее: [проект](http");
        link.PlainText.ShouldStartWith("Подробнее: проект: http");
    }

    [Fact]
    public void ReturnsNullForNullReference()
    {
        using var scope = factory.Services.CreateScope();
        var renderer = Resolve(scope);

        renderer.RenderEntityLink(null).ShouldBeNull();
    }
}
