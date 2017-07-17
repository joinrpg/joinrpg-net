using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
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

    [HttpGet]
    [Route("")]
    public async Task<IEnumerable<object>> GetList(int projectId, [FromUri] DateTime? modifiedSince = null)
    {
      return (await CharacterRepository.GetCharacterHeaders(projectId, modifiedSince)).Select(character =>
        new
        {
          character.CharacterId,
          character.UpdatedAt,
          character.IsActive,
          CharacterLink = $"/x-game-api/{projectId}/characters/{character.CharacterId}/"
        });
    }

    [HttpGet]
    [Route("{characterId}/")]
    public async Task<object> GetOne(int projectId, int characterId )
    {
      var character = await CharacterRepository.GetCharacterViewAsync(projectId, characterId);
      var project = await ProjectRepository.GetProjectWithFieldsAsync(projectId);
      return
        new
        {
          character.CharacterId,
          character.UpdatedAt,
          character.IsActive,
          character.InGame,
          BusyStatus = character.GetBusyStatus().ToString(),
          Groups = character.Groups.Where(group => group.IsActive && !group.IsSpecial).Select(
            group => new
            {
              group.CharacterGroupId,
              group.CharacterGroupName,
            }).OrderBy(group => group.CharacterGroupId),
          Fields = GetFields(character, project).Where(field => field.HasViewableValue).Select(field => new
          {
            field.Field.ProjectFieldId,
            field.Value,
            field.DisplayString
          }),
          character.ApprovedClaim?.PlayerUserId,
        };
    }

    private List<FieldWithValue> GetFields(CharacterView character, Project project)
    {
      var projectFields = project.GetFields().ToList();
      projectFields.FillFrom(character.ApprovedClaim);
      projectFields.FillFrom(character);
      return projectFields;
    }
  }
}