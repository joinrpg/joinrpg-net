using System.Collections.Generic;
using System.Threading.Tasks;
using JoinRpg.Web.CharacterGroups;
using JoinRpg.WebPortal.Managers.CharacterGroupList;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi
{
    [Route("/webapi/character-groups/[action]")]
    public class CharacterGroupsListController : ControllerBase
    {
        private readonly CharacteGroupListViewService viewService;

        public CharacterGroupsListController(CharacteGroupListViewService viewService)
        {
            this.viewService = viewService;
        }

        //TODO add caching here
        [HttpGet]
        public async Task<List<CharacterGroupDto>> GetCharacterGroups(int projectId) => await viewService.GetCharacterGroups(projectId);
    }
}
