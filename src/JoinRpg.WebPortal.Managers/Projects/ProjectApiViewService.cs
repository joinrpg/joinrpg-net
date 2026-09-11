using JoinRpg.Data.Interfaces;
using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.Markdown;
using JoinRpg.XGameApi.Contract;

namespace JoinRpg.WebPortal.Managers.Projects;

internal class ProjectApiViewService(
    IProjectMetadataRepository projectMetadataRepository,
    IProjectRepository projectRepository,
    ICurrentUserAccessor currentUserAccessor
        ) : IProjectApiViewService
{
    public async Task<ProjectFieldsMetadata> GetFieldsMetadata(ProjectIdentification projectId)
    {
        var project = await projectMetadataRepository.GetProjectMetadata(projectId);
        _ = project.RequestMasterAccess(currentUserAccessor);

        return new ProjectFieldsMetadata
        {
            ProjectId = project.ProjectId,
            ProjectName = project.ProjectName,
            Fields = MapFields(project),
        };
    }

    public async Task<ProjectOverview> GetOverview(ProjectIdentification projectId)
    {
        var project = await projectMetadataRepository.GetProjectMetadata(projectId);
        _ = project.RequestMasterAccess(currentUserAccessor);

        return new ProjectOverview
        {
            ProjectId = project.ProjectId,
            ProjectName = project.ProjectName,
            Fields = MapFields(project),
            Groups = project.Groups.Values
                .Where(group => group.IsActive)
                .Select(group => new ProjectOverviewGroup
                {
                    CharacterGroupId = group.Id.CharacterGroupId,
                    CharacterGroupName = group.Name,
                    DirectParentGroupIds = [.. group.DirectParentGroupIds.Select(id => id.CharacterGroupId)],
                    IsSpecial = group.IsSpecial,
                }),
        };
    }

    private static IEnumerable<JoinRpg.XGameApi.Contract.ProjectFieldInfo> MapFields(ProjectInfo project) =>
        project.SortedFields.Select(field =>
            new JoinRpg.XGameApi.Contract.ProjectFieldInfo
            {
                FieldName = field.Name,
                ProjectFieldId = field.Id.ProjectFieldId,
                IsActive = field.IsActive,
                FieldType = field.Type.ToString(),
                ProgrammaticValue = field.ProgrammaticValue,
                SpecialGroupId = field.SpecialGroupId?.CharacterGroupId,
                ValueList = field.SortedVariants.Select(variant =>
                    new JoinRpg.XGameApi.Contract.ProjectFieldVariant
                    {
                        ProjectFieldVariantId = variant.Id.ProjectFieldVariantId,
                        Label = variant.Label,
                        IsActive = variant.IsActive,
                        Description = variant.Description.ToHtmlString().Value,
                        MasterDescription =
                            variant.MasterDescription.ToHtmlString().Value,
                        ProgrammaticValue = variant.ProgrammaticValue,
                        CharacterGroupId = variant.CharacterGroupId?.CharacterGroupId,
                    }),
            });

    public async Task<IEnumerable<ProjectHeader>> GetActiveProjects(UserIdentification userId)
    {
        return (await projectRepository.GetPersonalizedProjectsBySpecification(ProjectListSpecification.MyActiveProjects(userId)))
            .Select(p => new ProjectHeader { ProjectId = p.ProjectId, ProjectName = p.ProjectName });
    }

    public async Task<IEnumerable<ProjectHeader>> ListMasterProjects(UserIdentification userId)
    {
        return (await projectRepository.GetPersonalizedProjectsBySpecification(ProjectListSpecification.ActiveWithMyMasterAccess(userId)))
            .Where(p => p.HasMyMasterAccess)
            .Select(p => new ProjectHeader { ProjectId = p.ProjectId, ProjectName = p.ProjectName });
    }
}
