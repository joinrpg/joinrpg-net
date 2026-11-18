using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Web.Claims.UnifiedGrid;
using JoinRpg.Web.ProjectCommon.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;

[Route("/webapi/claim-list/[action]")]
public class ClaimListController(IClaimListClient claimClient, IUnifiedGridClient claimGridClient) : ControllerBase
{
    [HttpGet]
    [MasterAuthorize]
    public async Task<IReadOnlyCollection<ClaimLinkViewModel>> GetClaims([FromQuery] ProjectIdentification projectId, [FromQuery] ClaimStatusSpec claimStatusSpec)
    {
        return await claimClient.GetClaims(projectId, claimStatusSpec);
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<UgPageViewModel>> GetGrid(
        [FromQuery] ProjectIdentification projectId,
        [FromQuery] UgScopeView scope,
        [FromQuery] UgStatusFilterView status,
        [FromQuery] bool onlyWithProblems = false,
        [FromQuery] CharacterGroupIdentification? characterGroupId = null,
        [FromQuery] UserIdentification? responsibleMasterId = null,
        [FromQuery] int page = 1)
    {
        // Мусорная строка в query иначе молча превратилась бы в «без фильтра». Id группы чужого
        // проекта разбирается нормально и даёт пустой грид в сервисе.
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }
        return await claimGridClient.GetGrid(
            projectId,
            scope,
            new UgFilterView(status, onlyWithProblems, characterGroupId, responsibleMasterId),
            page);
    }
}
