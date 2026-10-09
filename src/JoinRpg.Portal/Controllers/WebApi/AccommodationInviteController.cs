using JoinRpg.Domain;
using JoinRpg.Web.Accommodation;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;

[Route("/webapi/AccommodationInvite/[action]")]
public class AccommodationInviteController(
    IAccommodationInviteClient inviteClient,
    IAccommodationTypeClient typeClient) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AccommodationInviteTargetsViewModel>> GetInviteTargets(
        [FromQuery] ClaimIdentification claimId)
        => await inviteClient.GetInviteTargets(claimId);

    [HttpPost]
    public async Task<ActionResult> CreateInvite(
        [FromQuery] ClaimIdentification claimId,
        [FromQuery] AccommodationGroupIdentification target)
        => await NotAllowedToBadRequest(() => inviteClient.CreateInvite(claimId, target));

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<AccommodationInviteViewModel>>> GetInvites(
        [FromQuery] ClaimIdentification claimId,
        [FromQuery] InviteDirection direction)
        => Ok(await inviteClient.GetInvites(claimId, direction));

    [HttpPost]
    public async Task<ActionResult> AcceptInvite([FromQuery] AccommodationInviteIdentification inviteId)
        => await NotAllowedToBadRequest(() => inviteClient.AcceptInvite(inviteId));

    [HttpPost]
    public async Task<ActionResult> DeclineInvite([FromQuery] AccommodationInviteIdentification inviteId)
        => await NotAllowedToBadRequest(() => inviteClient.DeclineInvite(inviteId));

    [HttpPost]
    public async Task<ActionResult> CancelInvite([FromQuery] AccommodationInviteIdentification inviteId)
        => await NotAllowedToBadRequest(() => inviteClient.CancelInvite(inviteId));

    [HttpGet]
    public async Task<ActionResult<AccommodationTypeChoiceViewModel>> GetAccommodationTypes(
        [FromQuery] ClaimIdentification claimId)
        => await typeClient.GetAccommodationTypes(claimId);

    [HttpPost]
    public async Task<ActionResult> SetAccommodationType(
        [FromQuery] ClaimIdentification claimId,
        [FromQuery] AccommodationTypeIdentification typeId)
        => await NotAllowedToBadRequest(() => typeClient.SetAccommodationType(claimId, typeId));

    /// <summary>
    /// Причина отказа предназначена игроку, поэтому отдаём её текстом, а не 500-й.
    /// </summary>
    /// <remarks>
    /// Контроллер наследует <see cref="ControllerBase"/>, и <c>CaptureNoAccessExceptionFilter</c>
    /// его не накрывает. Тексты исключений проекта — английские и технические, поэтому русскую
    /// формулировку держим здесь, рядом с кодом ответа (как в <c>JoinMvcControllerBase.AddModelException</c>).
    /// </remarks>
    private async Task<ActionResult> NotAllowedToBadRequest(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (AccommodationInviteNotAllowedException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (ProjectDeactivatedException)
        {
            return BadRequest("Проект находится в архиве, изменить проживание уже нельзя");
        }
        catch (NoAccessToProjectException)
        {
            return BadRequest("Недостаточно прав для этого действия");
        }

        return Ok();
    }
}
