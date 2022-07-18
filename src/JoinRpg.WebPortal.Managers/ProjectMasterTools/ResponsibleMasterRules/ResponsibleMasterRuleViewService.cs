using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models.UserProfile;
using JoinRpg.Web.ProjectMasterTools.ResponsibleMaster;

namespace JoinRpg.WebPortal.Managers.ProjectMasterTools.ResponsibleMasterRules;
internal class ResponsibleMasterRuleViewService : IResponsibleMasterRuleClient
{
    private readonly IResponsibleMasterRulesRepository responsibleMasterRulesRepository;
    private readonly IRespMasterRuleService service;
    private readonly IProjectRepository projectRepository;

    public ResponsibleMasterRuleViewService(
        IResponsibleMasterRulesRepository responsibleMasterRulesRepository,
        IRespMasterRuleService service,
        IProjectRepository projectRepository)
    {
        this.responsibleMasterRulesRepository = responsibleMasterRulesRepository;
        this.service = service;
        this.projectRepository = projectRepository;
    }
    public async Task<ResponsibleMasterRuleListViewModel> GetResponsibleMasterRuleList(ProjectIdentification projectId)
    {
        var groups = await responsibleMasterRulesRepository.GetResponsibleMasterRules(projectId);

        var sortedGroups = groups
            .OrderByDescending(g => g, new Sorter())
            .Select(g => g.ToRespRuleViewModel())
            .ToList();

        var project = await projectRepository.GetProjectAsync(projectId);
        var defaultUser = project.GetDefaultResponsibleMaster();
        sortedGroups.Add(
            ResponsibleMasterRuleViewModel.CreateSpecial(
                UserLinks.Create(defaultUser)
            ));

        return new ResponsibleMasterRuleListViewModel(sortedGroups);
    }
    public async Task<ResponsibleMasterRuleListViewModel> AddResponsibleMasterRule(ProjectIdentification projectIdentification, int groupId, int masterId)
    {
        await service.AddRule(projectIdentification, groupId, masterId);
        return await GetResponsibleMasterRuleList(projectIdentification);
    }

    public async Task RemoveResponsibleMasterRule(ProjectIdentification projectId, int ruleId)
        => await service.RemoveRule(projectId, ruleId);

    private class Sorter : IComparer<CharacterGroup>
    {
        private readonly Dictionary<int, List<CharacterGroup>> parentGroupCache = new();
        public int Compare(CharacterGroup? x, CharacterGroup? y)
        {
            ArgumentNullException.ThrowIfNull(x);
            ArgumentNullException.ThrowIfNull(y);

            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (GetParents(x).Contains(y))
            {
                return 1; // If Y is parent of X, X should be after Y
            }

            if (GetParents(y).Contains(x))
            {
                return -1; // If X is parent of Y, Y should be after X
            }

            return Comparer<int>.Default.Compare(x.CharacterGroupId, y.CharacterGroupId);
        }

        private List<CharacterGroup> GetParents(CharacterGroup x)
        {
            if (parentGroupCache.TryGetValue(x.CharacterGroupId, out var groups))
            {
                return groups;
            }
            var parents = x.GetParentGroupsToTop().ToList();
            parentGroupCache.Add(x.CharacterGroupId, parents);
            return parents;
        }
    }
}
