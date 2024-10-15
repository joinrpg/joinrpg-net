using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Web.ProjectCommon;
using JoinRpg.WebPortal.Managers.CharacterGroupList;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;

[Route("/webapi/characters/[action]")]
[RequireMaster]
public class CharactersListController(CharacterListViewService viewService) : ControllerBase
{
    //TODO add caching here
    [HttpGet]
    public async Task<List<CharacterDto>> GetCharacters(int projectId) => await viewService.GetCharacters(projectId);

    [HttpGet]
    public async Task<List<CharacterDto>> GetTemplateCharacters(int projectId) => await viewService.GetTemplateCharacters(projectId);
}
