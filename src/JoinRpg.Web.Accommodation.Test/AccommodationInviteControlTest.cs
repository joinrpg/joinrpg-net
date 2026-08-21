using Bunit;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.Accommodation.Test;

public class AccommodationInviteControlTest : BunitContext
{
    private static readonly ProjectIdentification ProjectId = new(123);
    private static readonly ClaimIdentification ClaimId = new(ProjectId, 5);
    private static readonly AccommodationRequestIdentification SenderRequestId = new(ProjectId, 7);

    private readonly FakeAccommodationInviteClient client = new();

    public AccommodationInviteControlTest()
    {
        // bootstrap-select и диалоги подтверждения ходят в JS — в тестах он не нужен
        Services.AddSingleton<IAccommodationInviteClient>(client);
        JSInterop.Mode = JSRuntimeMode.Loose;
        SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
    }

    private static AccommodationInviteTargetViewModel SingleTarget(int claimId)
        => new(
            AccommodationTargetIdentification.From(new ClaimIdentification(ProjectId, claimId)),
            Text: $"Игрок {claimId}",
            ExtraSearch: $"Персонаж {claimId}",
            Subtext: "еще не выбрал тип проживания");

    private static AccommodationInviteTargetViewModel GroupTarget(int requestId)
        => new(
            AccommodationTargetIdentification.From(new AccommodationRequestIdentification(ProjectId, requestId)),
            Text: "Первый, Второй",
            ExtraSearch: "Персонаж-1, Персонаж-2",
            Subtext: "(группа проживающих)");

    private IRenderedComponent<AccommodationInviteControl> RenderControl()
        => Render<AccommodationInviteControl>(parameters => parameters.Add(c => c.ClaimId, ClaimId));

    [Fact]
    public void ShouldSayNobodyToInviteWhenTargetListEmpty()
    {
        client.NextTargets = new AccommodationInviteTargetsViewModel(SenderRequestId, RoomFreeSpace: 2, Targets: []);

        var cut = RenderControl();

        cut.Markup.ShouldContain("Пока еще нельзя никого пригласить");
    }

    [Fact]
    public void ShouldSayNobodyToInviteWhenRoomIsFull()
    {
        client.NextTargets = new AccommodationInviteTargetsViewModel(
            SenderRequestId, RoomFreeSpace: 0, Targets: [SingleTarget(11)]);

        var cut = RenderControl();

        cut.Markup.ShouldContain("Пока еще нельзя никого пригласить");
    }

    [Fact]
    public void ShouldRenderEveryTargetAsOption()
    {
        client.NextTargets = new AccommodationInviteTargetsViewModel(
            SenderRequestId, RoomFreeSpace: 3, Targets: [GroupTarget(7), SingleTarget(11)]);

        var cut = RenderControl();

        var options = cut.FindAll("select option");
        // Пустая строка «(выберите, кого пригласить)» плюс две цели
        options.Count.ShouldBe(3);
        cut.Markup.ShouldContain("(группа проживающих)");
        cut.Markup.ShouldContain("еще не выбрал тип проживания");
    }

    /// <summary>
    /// Группа проживающих кодируется отрицательным значением, отдельная заявка — положительным.
    /// Это единственное место, где кодировка видна снаружи <see cref="AccommodationTargetIdentification"/>.
    /// </summary>
    [Fact]
    public void GroupTargetShouldBeRenderedWithNegativeValue()
    {
        client.NextTargets = new AccommodationInviteTargetsViewModel(
            SenderRequestId, RoomFreeSpace: 3, Targets: [GroupTarget(7), SingleTarget(11)]);

        var cut = RenderControl();

        var values = cut.FindAll("select option")
            .Select(option => option.GetAttribute("value"))
            .ToArray();

        values.ShouldContain(AccommodationTargetIdentification
            .From(new AccommodationRequestIdentification(ProjectId, 7)).ToString());
        values.ShouldContain(AccommodationTargetIdentification
            .From(new ClaimIdentification(ProjectId, 11)).ToString());
    }

    [Fact]
    public void InviteButtonShouldBeDisabledUntilTargetSelected()
    {
        client.NextTargets = new AccommodationInviteTargetsViewModel(
            SenderRequestId, RoomFreeSpace: 3, Targets: [SingleTarget(11)]);

        var cut = RenderControl();

        cut.Find("button").HasAttribute("disabled").ShouldBeTrue();
    }


}
