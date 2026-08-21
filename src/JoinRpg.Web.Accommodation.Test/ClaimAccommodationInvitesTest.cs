using Bunit;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.Accommodation.Test;

public class ClaimAccommodationInvitesTest : BunitContext
{
    private static readonly ProjectIdentification ProjectId = new(123);
    private static readonly ClaimIdentification ClaimId = new(ProjectId, 5);

    private readonly FakeAccommodationInviteClient client = new();

    public ClaimAccommodationInvitesTest()
    {
        Services.AddSingleton<IAccommodationInviteClient>(client);
        Services.AddSingleton<IUriLocator<UserLinkViewModel>>(new FakeUserLinkLocator());
        JSInterop.Mode = JSRuntimeMode.Loose;
        SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
    }

    private static AccommodationInviteViewModel Invite(int inviteId, InviteState state)
        => new(
            new AccommodationInviteIdentification(ProjectId, inviteId),
            new UserLinkViewModel(42, "Сосед", ViewMode.Show),
            state);

    private IRenderedComponent<ClaimAccommodationInvites> RenderInvites(InviteDirection direction)
        => Render<ClaimAccommodationInvites>(parameters => parameters
            .Add(c => c.ClaimId, ClaimId)
            .Add(c => c.Direction, direction));

    [Fact]
    public void ShouldRenderNothingWhenNoInvites()
    {
        var cut = RenderInvites(InviteDirection.Incoming);

        cut.Markup.Trim().ShouldBeEmpty();
    }

    [Fact]
    public void IncomingPanelShouldOfferAcceptAndDecline()
    {
        client.NextInvites[InviteDirection.Incoming] = [Invite(1, InviteState.Unanswered)];

        var cut = RenderInvites(InviteDirection.Incoming);

        cut.Markup.ShouldContain("Полученные приглашения");
        cut.Markup.ShouldContain("Принять");
        cut.Markup.ShouldContain("Отклонить");
        cut.Markup.ShouldNotContain("Отозвать");
    }

    [Fact]
    public void OutgoingPanelShouldOfferCancelOnly()
    {
        client.NextInvites[InviteDirection.Outgoing] = [Invite(1, InviteState.Unanswered)];

        var cut = RenderInvites(InviteDirection.Outgoing);

        cut.Markup.ShouldContain("Отправленные приглашения");
        cut.Markup.ShouldContain("Отозвать");
        cut.Markup.ShouldNotContain("Принять");
    }

    /// <summary>
    /// Отвеченное приглашение показывается текстом состояния, а не кнопками.
    /// </summary>
    [Fact]
    public void AnsweredInviteShouldRenderStateInsteadOfButtons()
    {
        client.NextInvites[InviteDirection.Incoming] = [Invite(1, InviteState.Declined)];

        var cut = RenderInvites(InviteDirection.Incoming);

        cut.Markup.ShouldContain("text-danger");
        cut.Markup.ShouldContain("Отклонено");
        cut.FindAll("button").ShouldBeEmpty();
    }

    private sealed class FakeUserLinkLocator : IUriLocator<UserLinkViewModel>
    {
        public Uri GetUri(UserLinkViewModel target) => new($"https://example.com/user/{target.UserId}");
    }
}
