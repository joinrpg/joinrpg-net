using System.Reflection;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Portal.Controllers.WebApi;
using JoinRpg.Portal.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Test.Controllers;

/// <summary>
/// Настройки полей читает любой мастер (панель на странице настроек проекта), поэтому на контроллере
/// право не стоит — его несёт каждый изменяющий метод. Новый POST без права открылся бы молча.
/// </summary>
public class ProjectFieldOperationsControllerAuthorizationTest
{
    public static TheoryData<string> PostActions()
        => [.. typeof(ProjectFieldOperationsController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.GetCustomAttribute<HttpPostAttribute>() is not null)
            .Select(m => m.Name)];

    [Theory]
    [MemberData(nameof(PostActions))]
    public void PostRequiresCanChangeFields(string action)
    {
        var method = typeof(ProjectFieldOperationsController).GetMethod(action)!;

        method.GetCustomAttributes<RequireMaster>()
            .Select(a => a.Policy)
            .ShouldContain(new MasterAuthorize(Permission.CanChangeFields).Policy);
    }
}
