using System.Text.Json;

namespace JoinRpg.Common.PrimitiveTypes.Test;

// Атрибут [JsonConverter] эмиттится генератором, но заработает для потребителей только после того, как
// JoinRpg.Common.PrimitiveTypes.SourceGenerator будет переопубликован и версия пакета поднята (ADR011, PR4).
// До этого момента типы всё ещё сериализуются старым (вложенным) форматом — тесты временно скипнуты.
public class TypedEntityIdJsonConverterTest
{
    private const string SkipReason = "Требует переопубликованный JoinRpg.Common.PrimitiveTypes.SourceGenerator (ADR011, PR4)";

    [Fact(Skip = SkipReason)]
    public void ShouldSerializeAsSingleString()
    {
        var instance = new UserIdentification(42);

        var json = JsonSerializer.Serialize(instance);

        json.ShouldBe("\"UserId(42)\"");
    }

    [Fact(Skip = SkipReason)]
    public void ShouldDeserializeFromString()
    {
        var deserialized = JsonSerializer.Deserialize<UserIdentification>("\"UserId(42)\"");

        deserialized.ShouldBe(new UserIdentification(42));
    }

    [Fact(Skip = SkipReason)]
    public void ShouldThrowOnNonStringToken()
    {
        _ = Should.Throw<JsonException>(() => JsonSerializer.Deserialize<UserIdentification>("42"));
    }

    [Fact(Skip = SkipReason)]
    public void ShouldThrowOnUnparsableString()
    {
        _ = Should.Throw<JsonException>(() => JsonSerializer.Deserialize<UserIdentification>("\"not-an-id\""));
    }

    [Fact(Skip = SkipReason)]
    public void ShouldRoundTripAsDictionaryKey()
    {
        var dictionary = new Dictionary<UserIdentification, string>
        {
            [new UserIdentification(1)] = "first",
            [new UserIdentification(2)] = "second",
        };

        var json = JsonSerializer.Serialize(dictionary);
        var deserialized = JsonSerializer.Deserialize<Dictionary<UserIdentification, string>>(json);

        deserialized.ShouldBe(dictionary);
    }
}
