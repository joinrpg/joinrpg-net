using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Helpers.Test;

namespace JoinRpg.DomainTypes.Test;

public class JsonRoundTripTests
{
    [Fact]
    public void CaptainAccessRuleShouldRoundTripThroughJson()
    {
        var instance = new CaptainAccessRule(new CharacterGroupIdentification(1, 1), new UserIdentification(1), false);
        instance.ShouldRoundTripThroughJson();
    }
}
