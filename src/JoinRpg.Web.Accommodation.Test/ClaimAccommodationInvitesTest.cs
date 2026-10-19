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

    private static AccommodationInviteViewModel Invite(
        int inviteId,
        InviteState state,
        bool canRespond = true,
        string counterparty = "Сосед")
        => new(
            new AccommodationInviteIdentification(ProjectId, inviteId),
            new UserLinkViewModel(new UserIdentification(42 + inviteId), counterparty, ViewMode.Show),
            state,
            canRespond);

    private IRenderedComponent<ClaimAccommodationInvites> RenderInvites()
        => Render<ClaimAccommodationInvites>(parameters => parameters.Add(c => c.ClaimId, ClaimId));

    [Fact]
    public void ShouldRenderNothingWhenNoInvites()
    {
        var cut = RenderInvites();

        cut.Markup.Trim().ShouldBeEmpty();
    }

    [Fact]
    public void IncomingPanelShouldOfferAcceptAndDecline()
    {
        client.NextInvites[InviteDirection.Incoming] = [Invite(1, InviteState.Unanswered)];

        var cut = RenderInvites();

        cut.Markup.ShouldContain("Полученные приглашения");
        cut.Markup.ShouldContain("Принять");
        cut.Markup.ShouldContain("Отклонить");
        cut.Markup.ShouldNotContain("Отозвать");
    }

    [Fact]
    public void OutgoingPanelShouldOfferCancelOnly()
    {
        client.NextInvites[InviteDirection.Outgoing] = [Invite(1, InviteState.Unanswered)];

        var cut = RenderInvites();

        cut.Markup.ShouldContain("Отправленные приглашения");
        cut.Markup.ShouldContain("Отозвать");
        cut.Markup.ShouldNotContain("Принять");
    }

    /// <summary>
    /// Мастер видит приглашения игрока без кнопок: отвечает на них только сам игрок (#5656).
    /// </summary>
    [Theory]
    [InlineData(InviteDirection.Incoming)]
    [InlineData(InviteDirection.Outgoing)]
    public void MasterShouldSeeInvitesWithoutButtons(InviteDirection direction)
    {
        client.NextInvites[direction] = [Invite(1, InviteState.Unanswered, canRespond: false)];

        var cut = RenderInvites();

        cut.FindAll("button").ShouldBeEmpty();
        cut.Markup.ShouldContain("Не отвечено");
    }

    /// <summary>
    /// Оба направления в одном острове: каждая панель получает свой список (#5670).
    /// </summary>
    [Fact]
    public void EachPanelShouldShowItsOwnInvites()
    {
        client.NextInvites[InviteDirection.Incoming] = [Invite(1, InviteState.Unanswered, counterparty: "Пригласивший")];
        client.NextInvites[InviteDirection.Outgoing] = [Invite(2, InviteState.Unanswered, counterparty: "Приглашённый")];

        var cut = RenderInvites();

        var panels = cut.FindAll(".panel");
        panels.Count.ShouldBe(2);

        panels[0].TextContent.ShouldContain("Полученные приглашения");
        panels[0].TextContent.ShouldContain("Пригласивший");
        panels[0].TextContent.ShouldContain("Принять");
        panels[0].TextContent.ShouldNotContain("Приглашённый");
        panels[0].TextContent.ShouldNotContain("Отозвать");

        panels[1].TextContent.ShouldContain("Отправленные приглашения");
        panels[1].TextContent.ShouldContain("Приглашённый");
        panels[1].TextContent.ShouldContain("Отозвать");
        panels[1].TextContent.ShouldNotContain("Пригласивший");
        panels[1].TextContent.ShouldNotContain("Принять");
    }

    [Fact]
    public void ShouldNotRenderIncomingPanelWhenOnlyOutgoing()
    {
        client.NextInvites[InviteDirection.Outgoing] = [Invite(1, InviteState.Unanswered)];

        var cut = RenderInvites();

        cut.FindAll(".panel").Count.ShouldBe(1);
        cut.Markup.ShouldNotContain("Полученные приглашения");
    }

    /// <summary>
    /// Отвеченное приглашение показывается текстом состояния, а не кнопками.
    /// </summary>
    [Fact]
    public void AnsweredInviteShouldRenderStateInsteadOfButtons()
    {
        client.NextInvites[InviteDirection.Incoming] = [Invite(1, InviteState.Declined)];

        var cut = RenderInvites();

        cut.Markup.ShouldContain("text-danger");
        cut.Markup.ShouldContain("Отклонено");
        cut.FindAll("button").ShouldBeEmpty();
    }

    private sealed class FakeUserLinkLocator : IUriLocator<UserLinkViewModel>
    {
        public Uri GetUri(UserLinkViewModel target) => new($"https://example.com/user/{target.UserId!.Value}");
    }
}
