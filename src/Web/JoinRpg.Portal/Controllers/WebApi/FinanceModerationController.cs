using JoinRpg.Domain;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Web.Claims.Finance;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;

/// <summary>
/// Подтверждение и отклонение финансовой операции. Кто вправе — проверяет сервис; игроку здесь делать нечего.
/// </summary>
[Route("/webapi/{projectId}/finance-moderation/[action]")]
[RequireMaster]
public class FinanceModerationController(IFinanceModerationClient client) : ControllerBase
{
    /// <summary>Без <c>[ApiController]</c> пустое тело приходит null-ом — отказ, а не 500-я.</summary>
    private const string EmptyBody = "Пустой запрос. Обновите страницу и попробуйте ещё раз.";

    [HttpPost]
    public async Task<ActionResult> Approve(
        ProjectIdentification projectId,
        [FromQuery] int claimId,
        [FromQuery] int operationId,
        [FromBody] FinanceModerationDecisionRequest? request)
        => request is null
            ? BadRequest(EmptyBody)
            : await AlreadyModeratedToBadRequest(() => client.Approve(GetOperationId(projectId, claimId, operationId), request));

    [HttpPost]
    public async Task<ActionResult> Decline(
        ProjectIdentification projectId,
        [FromQuery] int claimId,
        [FromQuery] int operationId,
        [FromBody] FinanceModerationDecisionRequest? request)
    {
        if (request is null)
        {
            return BadRequest(EmptyBody);
        }
        if (string.IsNullOrWhiteSpace(request.CommentText))
        {
            return BadRequest("Напишите, почему операция отклонена — игрок увидит этот комментарий.");
        }

        return await AlreadyModeratedToBadRequest(() => client.Decline(GetOperationId(projectId, claimId, operationId), request));
    }

    private static FinanceOperationIdentification GetOperationId(ProjectIdentification projectId, int claimId, int operationId)
        => new(new ClaimIdentification(projectId, claimId), operationId);

    /// <summary>
    /// Операцию мог успеть обработать другой мастер — это штатный исход, а не 500-я.
    /// </summary>
    private async Task<ActionResult> AlreadyModeratedToBadRequest(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (ValueAlreadySetException)
        {
            return BadRequest("Эта операция уже подтверждена или отклонена. Обновите страницу.");
        }

        return Ok();
    }
}
