using JoinRpg.Data.Interfaces;
using JoinRpg.Web.CharacterGroups.GroupReport;

namespace JoinRpg.WebPortal.Managers.CharacterGroups;

internal class GroupReportViewService(IProjectRepository projectRepository) : IGroupReportClient
{
    public async Task<GroupReportViewModel?> GetReport(CharacterGroupIdentification groupId)
    {
        var field = await projectRepository.LoadGroupWithTreeAsync(groupId.ProjectId.Value, groupId.CharacterGroupId);
        if (field is null)
        {
            return null;
        }

        return GroupReportViewModelBuilder.Build(field, field.Project.Details.EnableCheckInModule);
    }
}
