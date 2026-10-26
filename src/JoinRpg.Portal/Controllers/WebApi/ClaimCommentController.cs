using JoinRpg.Web.Claims.Comments;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;

/// <summary>
/// Комментарии к заявке. Доступ к заявке (мастер или её игрок) проверяет сервис.
/// </summary>
[Route("/webapi/claim-comment/[action]")]
public class ClaimCommentController(IClaimCommentClient client) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult> AddComment([FromBody] AddClaimCommentRequest? request)
    {
        if (request is null)
        {
            return BadRequest("Пустой запрос. Обновите страницу и попробуйте ещё раз.");
        }
        if (string.IsNullOrWhiteSpace(request.CommentText))
        {
            return BadRequest("Напишите текст комментария.");
        }
        if (request.ParentCommentId is { } parent && parent.ClaimId != request.ClaimId)
        {
            return BadRequest("Комментарий, на который вы отвечаете, относится к другой заявке.");
        }

        await client.AddComment(request);
        return Ok();
    }
}
