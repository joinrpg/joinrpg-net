using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Portal.Controllers.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Portal.Test;

/// <summary>
/// Доменные исключения, которые пользователь может вызвать своими действиями, должны превращаться
/// в понятное сообщение на форме, а не в «Неожиданная ошибка, обратитесь в техподдержку».
/// </summary>
public class ModelExceptionMappingTest
{
    /// <summary>Открывает доступ к защищённому методу базового контроллера.</summary>
    private sealed class TestController : JoinMvcControllerBase
    {
        public new void AddModelException(Exception exception) => base.AddModelException(exception);
    }

    private static TestController CreateController()
    {
        var services = new ServiceCollection();
        _ = services.AddLogging();

        return new TestController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() },
            },
        };
    }

    [Fact]
    public void KnownDomainExceptionIsShownToUser()
    {
        var controller = CreateController();

        controller.AddModelException(
            new MasterHasResponsibleException(new ProjectIdentification(1), new UserIdentification(2)));

        controller.ModelState[""].ShouldNotBeNull().Errors.ShouldHaveSingleItem()
            .ErrorMessage.ShouldNotContain("Неожиданная ошибка");
    }

    /// <summary>
    /// Страж: неизвестные исключения по-прежнему попадают в общую ветку — иначе тест выше прошёл бы
    /// на любом сообщении.
    /// </summary>
    [Fact]
    public void UnknownExceptionFallsBackToSupportMessage()
    {
        var controller = CreateController();

        controller.AddModelException(new InvalidOperationException("что-то пошло не так"));

        controller.ModelState[""].ShouldNotBeNull().Errors.ShouldHaveSingleItem()
            .ErrorMessage.ShouldContain("Неожиданная ошибка");
    }
}
