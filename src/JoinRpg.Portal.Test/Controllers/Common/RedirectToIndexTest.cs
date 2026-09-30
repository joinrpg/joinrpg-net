using JoinRpg.DomainTypes;
using JoinRpg.Portal.Controllers.Common;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Test.Controllers.Common;

/// <summary>
/// Типизированный идентификатор, положенный в route values как есть, уезжает в URL своим
/// ToString() — получается путь вида "/project(5)/roles/10". Ссылка при этом рабочая (биндер
/// такое разбирает), поэтому поломка тихая: её видно только глазами в адресной строке.
/// </summary>
public class RedirectToIndexTest
{
    [Fact]
    public void RedirectToIndexByGroupId_PutsPlainNumbersIntoRouteValues()
    {
        var controller = new TestController();

        var result = controller.CallRedirectToIndex(new CharacterGroupIdentification(new ProjectIdentification(5), 10))
            .ShouldBeOfType<RedirectToActionResult>();

        result.ControllerName.ShouldBe("GameGroups");
        result.RouteValues.ShouldNotBeNull();
        result.RouteValues["ProjectId"].ShouldBe(5);
        result.RouteValues["CharacterGroupId"].ShouldBe(10);
    }

    private sealed class TestController : JoinControllerGameBase
    {
        public ActionResult CallRedirectToIndex(CharacterGroupIdentification characterGroupId)
            => RedirectToIndex(characterGroupId);
    }
}
