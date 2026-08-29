using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.Web.Models.ClaimList;

namespace JoinRpg.WebPortal.Models.Test;

public class ClaimListBuilderTest
{
    private MockedProject Mock { get; } = new MockedProject();

    // Регрессия для #4670: GetLastComment раньше вызывал User.GetUserInfo(), который
    // обращается к user.Auth без null-conditional. В реальном приложении это оборачивалось
    // в лишний lazy load почти на каждую заявку в списке; в этом тесте (без EF-контекста)
    // непрогруженный Auth равен null и вызов падает с NullReferenceException.
    [Fact]
    public void GetLastCommentDoesNotTouchUnloadedNavigationProperties()
    {
        var claim = Mock.CreateClaim(Mock.Character, Mock.Player);

        var result = ClaimListBuilder.GetLastComment(claim, AccessArguments.None);

        result.By.UserId.Value.ShouldBe(Mock.Player.UserId);
    }
}
