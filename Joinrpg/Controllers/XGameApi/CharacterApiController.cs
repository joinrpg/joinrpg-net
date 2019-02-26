using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Filter;
using JoinRpg.Web.Models.Characters;
using JoinRpg.Web.XGameApi.Contract;
using CharacterHeader = JoinRpg.Web.XGameApi.Contract.CharacterHeader;
using GroupHeader = JoinRpg.Web.XGameApi.Contract.GroupHeader;

namespace JoinRpg.Web.Controllers.XGameApi
{
    [RoutePrefix("x-game-api/{projectId}/characters"), XGameMasterAuthorize()]
    public class CharacterApiController : XGameApiController
    {
        private ICharacterRepository CharacterRepository { get; }
        private ICharacterService CharacterService { get; }

        public CharacterApiController(
            IProjectRepository projectRepository,
            ICharacterRepository characterRepository,
            ICharacterService characterService
            ) : base(projectRepository)
        {
            CharacterRepository = characterRepository;
            CharacterService = characterService;
        }

        /// <summary>
        /// Load character list. If you aggressively pull characters,
        /// please use modifiedSince parameter.
        /// </summary>
        [HttpGet]
        [Route("")]
        public async Task<IEnumerable<CharacterHeader>> GetList(int projectId,
            [FromUri]
            DateTime? modifiedSince = null)
        {
            return (await CharacterRepository.GetCharacterHeaders(projectId, modifiedSince)).Select(
                character =>
                    new CharacterHeader
                    {
                        CharacterId = character.CharacterId,
                        UpdatedAt = character.UpdatedAt,
                        IsActive = character.IsActive,
                        CharacterLink =
                            $"/x-game-api/{projectId}/characters/{character.CharacterId}/",
                    });
        }

        /// <summary>
        /// Character details
        /// </summary>
        [HttpGet]
        [Route("{characterId}/")]
        public async Task<CharacterInfo> GetOne(int projectId, int characterId)
        {
            var character = await CharacterRepository.GetCharacterViewAsync(projectId, characterId);
            var project = await ProjectRepository.GetProjectWithFieldsAsync(projectId);
            return
                new CharacterInfo
                {
                    CharacterId = character.CharacterId,
                    UpdatedAt = character.UpdatedAt,
                    IsActive = character.IsActive,
                    InGame = character.InGame,
                    BusyStatus = (CharacterBusyStatus) character.GetBusyStatus(),
                    Groups = ToGroupHeaders(character.DirectGroups),
                    AllGroups = ToGroupHeaders(character.AllGroups),
                    Fields = GetFields(character, project).Where(field => field.HasViewableValue)
                        .Select(field => new FieldValue
                        {
                            ProjectFieldId = field.Field.ProjectFieldId,
                            Value = field.Value,
                            DisplayString = field.DisplayString,
                        }),
                    PlayerUserId = character.ApprovedClaim?.PlayerUserId,
                    CharacterDescription = character.Description,
                    CharacterName = character.Name,
                };
        }

        /// <summary>
        /// Allows to set character fields as master
        /// <param name="fieldValues">Key = FieldId, Value = field value</param>
        /// </summary>
        [XGameMasterAuthorize()]
        [HttpPost]
        [Route("{characterId}/fields")]
        public async Task<string> SetCharacterFields(int projectId, int characterId, Dictionary<int, string> fieldValues)
        {
            await CharacterService.SetFields(projectId, characterId, fieldValues);
            return "ok";
        }

        private static IOrderedEnumerable<GroupHeader> ToGroupHeaders(
            IReadOnlyCollection<Data.Interfaces.GroupHeader> characterDirectGroups)
        {
            return characterDirectGroups.Where(group => group.IsActive && !group.IsSpecial)
                .Select(
                    group => new GroupHeader
                    {
                        CharacterGroupId = group.CharacterGroupId,
                        CharacterGroupName = group.CharacterGroupName,
                    })
                .OrderBy(group => group.CharacterGroupId);
        }

        private List<FieldWithValue> GetFields(CharacterView character, Project project)
        {
            var projectFields = project.GetFieldsNotFilled().ToList();
            projectFields.FillFrom(character.ApprovedClaim);
            projectFields.FillFrom(character);
            return projectFields;
        }
    }
}
