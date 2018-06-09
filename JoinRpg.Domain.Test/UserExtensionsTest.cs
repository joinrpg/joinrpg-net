using JoinRpg.DataModel;
using Shouldly; using Xunit;

namespace JoinRpg.Domain.Test
{

    public class UserExtensionsTest
    {
        [Fact]
        public void UserNameWithoutPrefferedName()
        {
            var user = new User
            {
                Email = "somebody@example.com",
            };
            user.GetDisplayName().ShouldBe("somebody");
        }
    }
}
