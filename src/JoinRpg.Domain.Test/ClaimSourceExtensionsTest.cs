using JoinRpg.DataModel;
using Shouldly;
using Xunit;

namespace JoinRpg.Domain.Test
{

    public class ClaimSourceExtensionsTest
    {
        [Fact]
        public void CharacterGroupIsNeverNpc() => new CharacterGroup().IsNpc().ShouldBeFalse();
    }
}
