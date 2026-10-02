using System.Text.Json;
using JoinRpg.DomainTypes;

namespace JoinRpg.Web.Plots.Test;

/// <summary>
/// <see cref="TargetsInfo"/> едет в Blazor-остров внутри <see cref="Elements.PlotElementEditModel"/>
/// и <see cref="Elements.PlotRenderedTextViewModel"/>, поэтому обязан переживать JSON round-trip.
/// </summary>
public class TargetsInfoJsonTest
{
    private static readonly ProjectIdentification ProjectId = new(123);

    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    [Fact]
    public void TargetsInfoSurvivesRoundTrip()
    {
        // Регрессия на DeserializeNoConstructor: у TargetsInfo появился второй публичный
        // конструктор (от CharacterInfo), и без [JsonConstructor] System.Text.Json не может
        // выбрать нужный — ровно то же, что случилось с AccommodationTypeViewModel (#5139).
        var original = new TargetsInfo(
            new List<CharacterTarget> { new(new CharacterIdentification(ProjectId, 11), "Вася") },
            new List<GroupTarget> { new(new CharacterGroupIdentification(ProjectId, 22), "Синие") });

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<TargetsInfo>(json, Options);

        restored.ShouldNotBeNull();
        restored.ShouldBeEquivalentTo(original);
    }
}
