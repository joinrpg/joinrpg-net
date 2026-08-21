using Bunit;
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

    private sealed class FakeAccommodationTypeClient : IAccommodationTypeClient
    {
        public AccommodationTypeChoiceViewModel NextChoice { get; set; } =
            new([], SelectedTypeId: null, RoomAssigned: false, HasNeighbours: false);

        public List<(ClaimIdentification ClaimId, AccommodationTypeIdentification TypeId)> Saved { get; } = [];

        public Task<AccommodationTypeChoiceViewModel> GetAccommodationTypes(ClaimIdentification claimId)
            => Task.FromResult(NextChoice);

        public Task SetAccommodationType(ClaimIdentification claimId, AccommodationTypeIdentification typeId)
        {
            Saved.Add((claimId, typeId));
            return Task.CompletedTask;
        }
    }
}
