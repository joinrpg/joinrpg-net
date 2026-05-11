using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Web.ProjectCommon;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;

[Route("/webapi/characters/[action]")]
[RequireMaster]
public class CharactersListController(ICharactersClient viewService) : ControllerBase
{
    //TODO add caching here
    [HttpGet]
    public async Task<List<CharacterDto>> GetCharactersByType(ProjectIdentification projectId, CharacterListType listType = CharacterListType.All) => await viewService.GetCharacters(projectId, listType);
}
