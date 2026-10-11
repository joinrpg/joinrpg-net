using JoinRpg.Helpers;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Web.ProjectMasterTools.ClaimRestore;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;

/// <summary>
/// Восстановление заявок из уведомлений (DRP, #5481). Проект заблокирован (ADR023), поэтому
/// <c>[ProjectShouldBeActive]</c> здесь нет: его проверяют сервисы.
/// </summary>
/// <remarks>
/// <c>[IgnoreAntiforgeryToken]</c> нет сознательно: остров присылает токен заголовком.
/// </remarks>
[ApiController]
[Route("/webapi/claim-restore/[action]")]
[RequireMaster(Permission.CanManageClaims)]
public class ClaimRestoreController(IClaimRestoreClient client) : ControllerBase
{
    [HttpGet]
    public async Task<ClaimRestorePlanViewModel> GetPlan([FromQuery] ProjectIdentification projectId)
        => await client.GetPlan(projectId);

    [HttpPost]
    public async Task<Results<Ok<ClaimRestoreApplyResult>, BadRequest<string>>> Apply(
        [FromQuery] ProjectIdentification projectId,
        [FromBody] ClaimRestoreApplyRequest request)
    {
        if (request.ClaimId.ProjectId != projectId || (request.Character is not null && request.Character.ProjectId != projectId))
        {
            return TypedResults.BadRequest("Неверный запрос: объект не найден в проекте");
        }
        try
        {
            return TypedResults.Ok(await client.Apply(request));
        }
        catch (FormatException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
        catch (JoinRpgBaseException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
        catch (ArgumentException ex)
        {
            // Сервис восстановления отвергает так неверный ввод (персонаж из другого проекта и т. п.).
            return TypedResults.BadRequest(ex.Message);
        }
    }
}
