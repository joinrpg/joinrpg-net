using System.Text.Json;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.DomainTypes.Test;

public class JsonRoundTripTests
{
    [Fact]
    public void CaptainAccessRuleShouldRoundTripThroughJson()
    {
        var instance = new CaptainAccessRule(new CharacterGroupIdentification(1, 1), new UserIdentification(1), false);
        var serialized = JsonSerializer.Serialize(instance).ShouldNotBeNull();
        var deserialized = JsonSerializer.Deserialize<CaptainAccessRule>(serialized).ShouldNotBeNull();
        deserialized.ShouldBe(instance);
    }
}
