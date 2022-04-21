using JoinRpg.Data.Interfaces;
using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.Web.CharacterGroups;

namespace JoinRpg.WebPortal.Managers.CharacterGroupList;

public class CharacteGroupListViewService : ICharacterGroupsClient
{
    private readonly IProjectRepository projectRepository;
    private readonly ICurrentUserAccessor currentUserAccessor;

    public CharacteGroupListViewService(IProjectRepository projectRepository, ICurrentUserAccessor currentUserAccessor)
    {
        this.projectRepository = projectRepository;
        this.currentUserAccessor = currentUserAccessor;
    }

    public async Task<List<CharacterGroupDto>> GetCharacterGroups(int projectId)
    {

        var rootGroup = (await projectRepository.LoadGroupWithTreeSlimAsync(projectId)) ?? throw new JoinRpgEntityNotFoundException(projectId, "project");

        var results = new CharacterGroupListGenerator(rootGroup, currentUserAccessor.UserId).Generate();
        return results;
    }
}
