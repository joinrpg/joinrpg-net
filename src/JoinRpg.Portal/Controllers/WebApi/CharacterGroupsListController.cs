using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Web.ProjectCommon;
using JoinRpg.WebPortal.Managers.CharacterGroupList;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;

[Route("/webapi/character-groups/[action]")]
[RequireMaster]
public class CharacterGroupsListController(CharacteGroupListViewService viewService) : ControllerBase
{

    //TODO add caching here
    [HttpGet]
    public async Task<List<CharacterGroupDto>> GetCharacterGroups(int projectId) => await viewService.GetCharacterGroups(projectId);
}
