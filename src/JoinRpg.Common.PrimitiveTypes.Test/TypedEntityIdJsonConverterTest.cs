using System.Text.Json;

namespace JoinRpg.Common.PrimitiveTypes.Test;

public class TypedEntityIdJsonConverterTest
{
    [Fact]
    public void ShouldSerializeAsSingleString()
    {
        var instance = new UserIdentification(42);

        var json = JsonSerializer.Serialize(instance);

        json.ShouldBe("\"UserId(42)\"");
    }

    [Fact]
    public void ShouldDeserializeFromString()
    {
        var deserialized = JsonSerializer.Deserialize<UserIdentification>("\"UserId(42)\"");

        deserialized.ShouldBe(new UserIdentification(42));
    }

    [Fact]
    public void ShouldThrowOnNonStringToken()
    {
        _ = Should.Throw<JsonException>(() => JsonSerializer.Deserialize<UserIdentification>("42"));
    }

    [Fact]
    public void ShouldThrowOnUnparsableString()
    {
        _ = Should.Throw<JsonException>(() => JsonSerializer.Deserialize<UserIdentification>("\"not-an-id\""));
    }

    [Fact]
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
