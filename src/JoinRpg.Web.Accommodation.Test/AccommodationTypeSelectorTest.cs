using Bunit;
using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.Accommodation.Test;

public class AccommodationTypeSelectorTest : BunitContext
{
    private static readonly ProjectIdentification ProjectId = new(123);
    private static readonly ClaimIdentification ClaimId = new(ProjectId, 5);

    private readonly FakeAccommodationTypeClient client = new();

    public AccommodationTypeSelectorTest()
    {
        Services.AddSingleton<IAccommodationTypeClient>(client);
        JSInterop.Mode = JSRuntimeMode.Loose;
        SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
    }

    private static AccommodationTypeViewModel Type(int id, string name)
        => new(new AccommodationTypeIdentification(ProjectId, id), name, Capacity: 4, Cost: 1000, "<p>Описание</p>");

    private IRenderedComponent<AccommodationTypeSelector> RenderSelector()
        => Render<AccommodationTypeSelector>(parameters => parameters.Add(c => c.ClaimId, ClaimId));

    [Fact]
    public void ShouldOfferToChooseWhenNothingSelectedYet()
    {
        client.NextChoice = new AccommodationTypeChoiceViewModel(
            [Type(1, "Палатка")], SelectedTypeId: null, RoomAssigned: false, HasNeighbours: false);

        var cut = RenderSelector();

        cut.Markup.ShouldContain("Выбрать");
        cut.Markup.ShouldNotContain("Изменить");
    }

    [Fact]
    public void ShouldOfferToChangeWhenTypeAlreadySelected()
    {
        client.NextChoice = new AccommodationTypeChoiceViewModel(
            [Type(1, "Палатка")],
            SelectedTypeId: new AccommodationTypeIdentification(ProjectId, 1),
            RoomAssigned: false,
            HasNeighbours: false);

        var cut = RenderSelector();

        cut.Markup.ShouldContain("Изменить");
    }

    [Fact]
    public void ShouldRenderRadioPerAvailableType()
    {
        client.NextChoice = new AccommodationTypeChoiceViewModel(
            [Type(1, "Палатка"), Type(2, "Домик")],
            SelectedTypeId: new AccommodationTypeIdentification(ProjectId, 2),
            RoomAssigned: false,
            HasNeighbours: false);

        var cut = RenderSelector();

        var radios = cut.FindAll("input[type=radio]");
        radios.Count.ShouldBe(2);
        cut.Markup.ShouldContain("Палатка");
        cut.Markup.ShouldContain("Домик");
        // Описание приходит уже отрисованным из Markdown и вставляется как разметка
        cut.Markup.ShouldContain("<p>Описание</p>");
    }

    [Fact]
    public void ShouldWarnAboutEvictionWhenRoomAssigned()
    {
        client.NextChoice = new AccommodationTypeChoiceViewModel(
            [Type(1, "Палатка")], SelectedTypeId: null, RoomAssigned: true, HasNeighbours: true);

        var cut = RenderSelector();

        cut.Markup.ShouldContain("выселит вас");
        // Предупреждение про соседей менее важное, показываем только одно
        cut.Markup.ShouldNotContain("отменит существующие договоренности");
    }

    [Fact]
    public void ShouldWarnAboutNeighboursWhenRoomNotAssignedYet()
    {
        client.NextChoice = new AccommodationTypeChoiceViewModel(
            [Type(1, "Палатка")], SelectedTypeId: null, RoomAssigned: false, HasNeighbours: true);

        var cut = RenderSelector();

        cut.Markup.ShouldContain("отменит существующие договоренности");
    }

    private async Task<IRenderedComponent<AccommodationTypeSelector>> SaveSelectedType()
    {
        client.NextChoice = new AccommodationTypeChoiceViewModel(
            [Type(1, "Палатка")],
            SelectedTypeId: new AccommodationTypeIdentification(ProjectId, 1),
            RoomAssigned: false,
            HasNeighbours: false);
        var cut = RenderSelector();

        await cut.ClickAndConfirm("Изменить…", "Варианты проживания", "Сохранить проживание");
        return cut;
    }

    /// <summary>
    /// Причину отказа сервера (например, проект в архиве) игрок видит как есть (#5735).
    /// </summary>
    [Fact]
    public async Task RefusalReasonIsShown()
    {
        client.Failure = new ServerProblemException("Проект находится в архиве, изменить проживание уже нельзя");

        var cut = await SaveSelectedType();

        cut.WaitForAssertion(() => VisibleError(cut).ShouldBe("Проект находится в архиве, изменить проживание уже нельзя"));
        client.Saved.Count.ShouldBe(1);
    }

    [Fact]
    public async Task UnexpectedFailureShowsGenericText()
    {
        client.Failure = new HttpRequestException("Response status code does not indicate success: 502 (Bad Gateway).");

        var cut = await SaveSelectedType();

        cut.WaitForAssertion(() => VisibleError(cut).ShouldBe("Не удалось сохранить тип проживания. Попробуйте ещё раз."));
        cut.Markup.ShouldNotContain("Bad Gateway");
    }

    /// <summary>
    /// Сохранение идёт после закрытия диалога: ошибка внутри закрытого &lt;dialog&gt; игроку не видна.
    /// </summary>
    private static string VisibleError(IRenderedComponent<AccommodationTypeSelector> cut)
    {
        var alert = cut.FindAll(".alert-danger").ShouldHaveSingleItem();
        alert.Closest("dialog").ShouldBeNull();
        return alert.TextContent.Trim();
    }

    private sealed class FakeAccommodationTypeClient : IAccommodationTypeClient
    {
        public AccommodationTypeChoiceViewModel NextChoice { get; set; } =
            new([], SelectedTypeId: null, RoomAssigned: false, HasNeighbours: false);

        public List<(ClaimIdentification ClaimId, AccommodationTypeIdentification TypeId)> Saved { get; } = [];

        /// <summary>Чем завершается сохранение. <c>null</c> — успехом.</summary>
        public Exception? Failure { get; set; }

        public Task<AccommodationTypeChoiceViewModel> GetAccommodationTypes(ClaimIdentification claimId)
            => Task.FromResult(NextChoice);

        public Task SetAccommodationType(ClaimIdentification claimId, AccommodationTypeIdentification typeId)
        {
            Saved.Add((claimId, typeId));
            return Failure is null ? Task.CompletedTask : Task.FromException(Failure);
        }
    }
}
