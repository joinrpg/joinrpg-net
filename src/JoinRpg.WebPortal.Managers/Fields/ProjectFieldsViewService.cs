using JoinRpg.Data.Interfaces;
using JoinRpg.Web.ProjectCommon.Fields;

namespace JoinRpg.WebPortal.Managers.Fields;

internal class ProjectFieldsViewService(IProjectMetadataRepository projectMetadataRepository) : IProjectFieldsClient
{
    public async Task<List<ProjectFieldDto>> GetProjectFields(ProjectIdentification projectId)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
        return projectInfo.SortedActiveFields
            .Select(f => new ProjectFieldDto(
                FieldId: f.Id,
                Name: f.Name,
                FieldType: f.Type,
                BoundTo: f.BoundTo))
            .ToList();
    }
}
