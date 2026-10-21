using JoinRpg.Helpers;
using JoinRpg.Portal.Infrastructure;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Web.ProjectMasterTools.ClaimRestore;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;

/// <summary>
/// Восстановление ручных оплат из уведомлений (DRP, #5631). Проект заблокирован (ADR023),
/// поэтому <c>[ProjectShouldBeActive]</c> здесь нет: его проверяют сервисы.
/// </summary>
/// <remarks>
/// <para><c>[IgnoreAntiforgeryToken]</c> нет сознательно: остров присылает токен заголовком.</para>
/// <para>
/// Атрибут требует то же право, что страница и соседние контроллеры, а не <c>CanManageMoney</c>: право на деньги
/// проверяет сервис (план отдаёт «нет доступа», применение и создание наличных — отказ). Иначе атрибут ответил бы
/// 403 с пустым телом, и панель вместо «нет доступа» показала бы ошибку.
/// </para>
/// </remarks>
[ApiController]
[Route("/webapi/finance-restore/[action]")]
[RequireMaster(Permission.CanManageClaims)]
public class FinanceRestoreController(IFinanceRestoreClient client) : ControllerBase
{
    private const string WrongProject = "Неверный запрос: заявка не из этого проекта";

    [HttpGet]
    public async Task<FinanceRestorePlanViewModel> GetPlan([FromQuery] ProjectIdentification projectId)
        => await client.GetPlan(projectId);

    [HttpPost]
    public async Task<Results<Ok<FinanceRestoreApplyResult>, BadRequest<string>>> Apply(
        [FromQuery] ProjectIdentification projectId,
        [FromBody] FinanceRestoreApplyRequest request)
    {
        if (request.ClaimId.ProjectId != projectId)
        {
            return TypedResults.BadRequest(WrongProject);
        }
        try
        {
            return TypedResults.Ok(await client.Apply(request));
        }
        // Проект заблокирован, в архиве, нет прав — WebApiExceptionHandler отдаст их по-русски с 409/403
        catch (Exception ex) when (ex is JoinRpgBaseException or ArgumentException && !WebApiExceptionHandler.IsExpectedRefusal(ex))
        {
            // Отказ сервиса — текстом, его показывают мастеру в строке плана.
            return TypedResults.BadRequest(ex.Message);
        }
    }

    [HttpPost]
    public async Task<Results<Ok, BadRequest<string>>> CreateOwnCash([FromQuery] ProjectIdentification projectId)
    {
        try
        {
            await client.CreateOwnCash(projectId);
            return TypedResults.Ok();
        }
        catch (Exception ex) when (ex is JoinRpgBaseException or ArgumentException && !WebApiExceptionHandler.IsExpectedRefusal(ex))
        {
            return TypedResults.BadRequest(ex.Message);
        }
    }
}
