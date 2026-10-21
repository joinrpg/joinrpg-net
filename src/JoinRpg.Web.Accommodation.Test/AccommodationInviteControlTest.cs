using Bunit;
using JoinRpg.Common.WebComponents;
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
            AccommodationGroupIdentification.From(new ClaimIdentification(ProjectId, claimId)),
            Text: $"Игрок {claimId}",
            ExtraSearch: $"Персонаж {claimId}",
            Subtext: "еще не выбрал тип проживания");

    private static AccommodationInviteTargetViewModel GroupTarget(int requestId)
        => new(
            AccommodationGroupIdentification.From(new AccommodationRequestIdentification(ProjectId, requestId)),
            Text: "Первый, Второй",
            ExtraSearch: "Персонаж-1, Персонаж-2",
            Subtext: "(группа проживающих)");

    private IRenderedComponent<AccommodationInviteControl> RenderControl()
        => Render<AccommodationInviteControl>(parameters => parameters.Add(c => c.ClaimId, ClaimId));

    [Fact]
    public void ShouldSayNobodyToInviteWhenTargetListEmpty()
    {
        client.NextTargets = new AccommodationInviteTargetsViewModel(SenderRequestId, RoomFreeSpace: 2, Targets: [], AccommodationInviteMode.Invite);

        var cut = RenderControl();

        cut.Markup.ShouldContain("Пока еще нельзя никого пригласить");
    }

    [Fact]
    public void ShouldShowNothingWhenRoomIsFull()
    {
        // Заполненность уже видна в панели проживания, а у типа на одного мест нет никогда —
        // «пока еще нельзя» там вводит в заблуждение (#5644)
        client.NextTargets = new AccommodationInviteTargetsViewModel(
            SenderRequestId, RoomFreeSpace: 0, Targets: [SingleTarget(11)], AccommodationInviteMode.Invite);

        var cut = RenderControl();

        cut.Markup.ShouldNotContain("Пока еще нельзя никого пригласить");
        cut.FindAll("select").ShouldBeEmpty();
        cut.FindAll("button").ShouldBeEmpty();
    }

    [Fact]
    public void ShouldRenderEveryTargetAsOption()
    {
        client.NextTargets = new AccommodationInviteTargetsViewModel(
            SenderRequestId, RoomFreeSpace: 3, Targets: [GroupTarget(7), SingleTarget(11)], AccommodationInviteMode.Invite);

        var cut = RenderControl();

        var options = cut.FindAll("select option");
        // Пустая строка «(выберите, кого пригласить)» плюс две цели
        options.Count.ShouldBe(3);
        cut.Markup.ShouldContain("(группа проживающих)");
        cut.Markup.ShouldContain("еще не выбрал тип проживания");
    }

    /// <summary>
    /// Группа проживающих кодируется отрицательным значением, отдельная заявка — положительным.
    /// Это единственное место, где кодировка видна снаружи <see cref="AccommodationGroupIdentification"/>.
    /// </summary>
    [Fact]
    public void GroupTargetShouldBeRenderedWithNegativeValue()
    {
        client.NextTargets = new AccommodationInviteTargetsViewModel(
            SenderRequestId, RoomFreeSpace: 3, Targets: [GroupTarget(7), SingleTarget(11)], AccommodationInviteMode.Invite);

        var cut = RenderControl();

        var values = cut.FindAll("select option")
            .Select(option => option.GetAttribute("value"))
            .ToArray();

        values.ShouldContain(AccommodationGroupIdentification
            .From(new AccommodationRequestIdentification(ProjectId, 7)).ToString());
        values.ShouldContain(AccommodationGroupIdentification
            .From(new ClaimIdentification(ProjectId, 11)).ToString());
    }

    [Fact]
    public void InviteButtonShouldBeDisabledUntilTargetSelected()
    {
        client.NextTargets = new AccommodationInviteTargetsViewModel(
            SenderRequestId, RoomFreeSpace: 3, Targets: [SingleTarget(11)], AccommodationInviteMode.Invite);

        var cut = RenderControl();

        cut.Find("button").HasAttribute("disabled").ShouldBeTrue();
    }

    /// <summary>
    /// Игрок приглашает, мастер с правом расселять — поселяет вместе (#5656).
    /// </summary>
    [Fact]
    public void PlayerShouldSeeInviteButton()
    {
        client.NextTargets = new AccommodationInviteTargetsViewModel(
            SenderRequestId, RoomFreeSpace: 3, Targets: [SingleTarget(11)], AccommodationInviteMode.Invite);

        var cut = RenderControl();

        cut.Find("button").TextContent.ShouldContain("Пригласить");
        cut.Markup.ShouldNotContain("Поселить вместе");
    }

    /// <inheritdoc cref="PlayerShouldSeeInviteButton"/>
    [Fact]
    public void MasterShouldSeeMergeButtonInsteadOfInvite()
    {
        client.NextTargets = new AccommodationInviteTargetsViewModel(
            SenderRequestId, RoomFreeSpace: 3, Targets: [SingleTarget(11)], AccommodationInviteMode.Merge);

        var cut = RenderControl();

        cut.Find("button").TextContent.ShouldContain("Поселить вместе");
        cut.Markup.ShouldNotContain("Пригласить");
        cut.Markup.ShouldContain("(выберите, кого поселить вместе)");
    }

    [Fact]
    public void MasterShouldSeeNobodyToMergeWhenTargetListEmpty()
    {
        client.NextTargets = new AccommodationInviteTargetsViewModel(
            SenderRequestId, RoomFreeSpace: 2, Targets: [], AccommodationInviteMode.Merge);

        var cut = RenderControl();

        cut.Markup.ShouldContain("Пока некого поселить вместе.");
        cut.Markup.ShouldNotContain("пригласить");
    }

    /// <summary>
    /// Ответственный мастер без права расселять не приглашает и не поселяет — контрол молчит.
    /// </summary>
    [Fact]
    public void ShouldShowNothingWhenNothingAllowed()
    {
        client.NextTargets = new AccommodationInviteTargetsViewModel(
            SenderRequestId, RoomFreeSpace: 3, Targets: [SingleTarget(11)], AccommodationInviteMode.None);

        var cut = RenderControl();

        cut.FindAll("select").ShouldBeEmpty();
        cut.FindAll("button").ShouldBeEmpty();
        cut.Markup.ShouldNotContain("Пока");
    }

    private async Task<IRenderedComponent<AccommodationInviteControl>> InviteSingleTarget(AccommodationInviteMode mode)
    {
        var target = SingleTarget(11);
        client.NextTargets = new AccommodationInviteTargetsViewModel(SenderRequestId, RoomFreeSpace: 3, Targets: [target], mode);
        var module = JSInterop.SetupModule("/_content/JoinRpg.Common.WebComponents/component-interop.js");
        _ = module.Setup<List<string>>("getSelectedValues", _ => true).SetResult([target.TargetId.ToString()]);

        var cut = RenderControl();
        cut.Find("select").Change(target.TargetId.ToString());

        if (mode == AccommodationInviteMode.Merge)
        {
            await cut.ClickAndConfirm("Поселить вместе", "Объединить в одну группу проживания?", "Поселить вместе");
        }
        else
        {
            await cut.ClickAndConfirm("Пригласить", "Отправить приглашение", "Пригласить");
        }
        return cut;
    }

    /// <summary>
    /// Причину отказа сервера (например, проект в архиве) игрок видит как есть (#5735).
    /// </summary>
    [Fact]
    public async Task InviteRefusalReasonIsShown()
    {
        client.Failure = new ServerProblemException("Проект находится в архиве, изменить проживание уже нельзя");

        var cut = await InviteSingleTarget(AccommodationInviteMode.Invite);

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Проект находится в архиве, изменить проживание уже нельзя"));
        client.CreatedInvites.Count.ShouldBe(1);
    }

    [Fact]
    public async Task InviteUnexpectedFailureShowsGenericText()
    {
        client.Failure = new HttpRequestException("Response status code does not indicate success: 502 (Bad Gateway).");

        var cut = await InviteSingleTarget(AccommodationInviteMode.Invite);

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Не удалось отправить приглашение. Попробуйте ещё раз."));
        cut.Markup.ShouldNotContain("Bad Gateway");
    }

    [Fact]
    public async Task MergeUnexpectedFailureShowsGenericText()
    {
        client.Failure = new HttpRequestException("Response status code does not indicate success: 502 (Bad Gateway).");

        var cut = await InviteSingleTarget(AccommodationInviteMode.Merge);

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Не удалось поселить вместе. Попробуйте ещё раз."));
        cut.Markup.ShouldNotContain("Bad Gateway");
        client.MergedGroups.Count.ShouldBe(1);
    }
}
