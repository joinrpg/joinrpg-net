using JoinRpg.Helpers;
using JoinRpg.Portal.Infrastructure;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Web.ProjectMasterTools.ClaimRestore;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;

/// <summary>
/// Восстановление комментариев к заявкам из уведомлений (DRP, #5492). Проект заблокирован (ADR023),
/// поэтому <c>[ProjectShouldBeActive]</c> здесь нет: его проверяют сервисы.
/// </summary>
/// <remarks><c>[IgnoreAntiforgeryToken]</c> нет сознательно: остров присылает токен заголовком.</remarks>
[ApiController]
[Route("/webapi/comment-restore/[action]")]
[RequireMaster(Permission.CanManageClaims)]
public class CommentRestoreController(ICommentRestoreClient client) : ControllerBase
{
    private const string WrongProject = "Неверный запрос: заявка не из этого проекта";

    [HttpGet]
    public async Task<CommentRestorePlanViewModel> GetPlan([FromQuery] ProjectIdentification projectId)
        => await client.GetPlan(projectId);

    [HttpPost]
    public async Task<Results<Ok<CommentRestoreApplyResult>, BadRequest<string>>> Apply(
        [FromQuery] ProjectIdentification projectId,
        [FromBody] ClaimIdentification claimId)
    {
        if (claimId.ProjectId != projectId)
        {
            return TypedResults.BadRequest(WrongProject);
        }
        try
        {
            return TypedResults.Ok(await client.Apply(claimId));
        }
        // Проект заблокирован, в архиве, нет прав — WebApiExceptionHandler отдаст их по-русски с 409/403
        catch (Exception ex) when (ex is JoinRpgBaseException or ArgumentException && !WebApiExceptionHandler.IsExpectedRefusal(ex))
        {
            // Отказ сервиса — текстом, его показывают мастеру в строке плана.
            return TypedResults.BadRequest(ex.Message);
        }
    }
}
