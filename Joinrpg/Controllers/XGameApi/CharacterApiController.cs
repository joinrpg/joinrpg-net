using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using JoinRpg.Data.Interfaces;
using JoinRpg.Web.Filter;
using JoinRpg.Web.Models;

namespace JoinRpg.Web.Controllers.XGameApi
{
  [RoutePrefix("x-game-api/{projectId}/characters"), XGameAuthorize()]
  public class CharacterApiController : XGameApiController
  {
    private ICharacterRepository CharacterRepository { get; }
    public CharacterApiController(IProjectRepository projectRepository, ICharacterRepository characterRepository) : base(projectRepository)
    {
      CharacterRepository = characterRepository;
    }

    [Route("get/{modifiedSince?}")]
    public async Task<IEnumerable<object>> Get(int projectId, DateTime? modifiedSince = null)
    {
      return (await CharacterRepository.GetCharacterIds(projectId, modifiedSince)).Select(id =>
        new
        {
          CharacterId = id,
          CharacterLink = $"/x-game-api/{projectId}/characters/{id}/details"
        });
    }
  }
}