using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using JoinRpg.Data.Interfaces;
using JoinRpg.Domain;
using JoinRpg.Web.Filter;
using JoinRpg.Web.Models.Characters;

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
    public async Task<IEnumerable<object>> GetList(int projectId, DateTime? modifiedSince = null)
    {
      return (await CharacterRepository.GetCharacterHeaders(projectId, modifiedSince)).Select(character =>
        new
        {
          character.CharacterId,
          character.UpdatedAt,
          character.IsActive,
          CharacterLink = $"/x-game-api/{projectId}/characters/{character.CharacterId}/details"
        });
    }

    [Route("{characterId}/details")]
    public async Task<object> GetOne(int projectId, int characterId )
    {
      var character = await CharacterRepository.GetCharacterWithDetails(projectId, characterId);
      return
        new
        {
          character.CharacterId,
          BusyStatus = character.GetBusyStatus().ToString(),
          Groups = character.Groups.Where(group => group.IsActive && !group.IsSpecial).Select(
            group => new
            {
              group.CharacterGroupId,
              group.CharacterGroupName,
            }).OrderBy(group => group.CharacterGroupId),
          Fields = character.GetFields().Where(field => field.HasViewableValue).Select(field => new
          {
            field.Field.ProjectFieldId,
            field.Value,
            field.DisplayString
          }),
          character.ApprovedClaim?.PlayerUserId,
          character.IsActive
        };
    }
  }
}