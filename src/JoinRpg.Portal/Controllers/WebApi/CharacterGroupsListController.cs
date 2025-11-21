using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Web.ProjectCommon;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;

[Route("/webapi/character-groups/[action]")]
[RequireMaster]
public class CharacterGroupsListController(ICharacterGroupsClient viewService) : ControllerBase
{

    //TODO add caching here
    [HttpGet]
    public async Task<List<CharacterGroupDto>> GetRealCharacterGroups(int projectId) => await viewService.GetRealCharacterGroups(projectId);

    //TODO add caching here
    [HttpGet]
    public async Task<List<CharacterGroupDto>> GetCharacterGroupsWithSpecial(int projectId) => await viewService.GetCharacterGroupsWithSpecial(projectId);
}
