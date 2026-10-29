using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Web.ProjectMasterTools.Finance;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;

/// <summary>
/// Панели страницы «Настройка финансов». Право на деньги проекта проверяет сервис.
/// </summary>
/// <remarks>
/// <c>[IgnoreAntiforgeryToken]</c> здесь сознательно нет: остров присылает токен заголовком,
/// и глобальный фильтр antiforgery закрывает и эти ручки.
/// </remarks>
[Route("/webapi/{projectId}/finance-settings/[action]")]
[RequireMaster]
[ApiController]
public class FinanceSettingsController(IFinanceSettingsClient client) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<FinanceGeneralSettingsViewModel>> GetGeneralSettings(ProjectIdentification projectId)
        => Ok(await client.GetGeneralSettings(projectId));

    [HttpPost]
    public async Task<ActionResult> SaveGeneralSettings(
        ProjectIdentification projectId,
        FinanceGeneralSettingsViewModel model)
    {
        model.ProjectId = projectId;
        await client.SaveGeneralSettings(model);
        return Ok();
    }
}
