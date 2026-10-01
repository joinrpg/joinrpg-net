using System.Text.Json;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.Accommodation.Test;

/// <summary>
/// Контракт JSON между сервером (<c>AccommodationInviteController</c>) и Blazor WASM-клиентом
/// (<c>AccommodationInviteClient</c>). Обе стороны используют <c>JsonSerializerDefaults.Web</c>.
/// </summary>
public class AccommodationTypeJsonTest
{
    private static readonly ProjectIdentification ProjectId = new(123);

    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    [Fact]
    public void ChoiceViewModelSurvivesRoundTrip()
    {
        // Регрессия на DeserializeNoConstructor: у AccommodationTypeViewModel два публичных
        // конструктора, без [JsonConstructor] System.Text.Json в WASM не мог выбрать нужный
        // и весь селектор типа проживания падал при загрузке.
        var original = new AccommodationTypeChoiceViewModel(
            // List, а не коллекционное выражение: ShouldBeEquivalentTo сравнивает в том числе
            // рантайм-тип коллекции, а десериализатор всегда отдаёт List.
            new List<AccommodationTypeViewModel> { new AccommodationTypeViewModel(
                new AccommodationTypeIdentification(ProjectId, 11),
                "Домик на четверых",
                Capacity: 4,
                Cost: 2500,
                "<p>Домик</p>") },
            SelectedTypeId: new AccommodationTypeIdentification(ProjectId, 11),
            RoomAssigned: true,
            HasNeighbours: false);

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<AccommodationTypeChoiceViewModel>(json, Options);

        restored.ShouldNotBeNull();
        restored.ShouldBeEquivalentTo(original);
    }
}
