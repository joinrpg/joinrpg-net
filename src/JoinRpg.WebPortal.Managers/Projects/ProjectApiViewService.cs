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
            Fields = project.SortedFields.Select(field =>
                new JoinRpg.XGameApi.Contract.ProjectFieldInfo
                {
                    FieldName = field.Name,
                    ProjectFieldId = field.Id.ProjectFieldId,
                    IsActive = field.IsActive,
                    FieldType = field.Type.ToString(),
                    ProgrammaticValue = field.ProgrammaticValue,
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
                        }),
                }),
        };
    }

    public async Task<IEnumerable<ProjectHeader>> GetActiveProjects(UserIdentification userId)
    {
        return (await projectRepository.GetPersonalizedProjectsBySpecification(ProjectListSpecification.MyActiveProjects(userId)))
            .Select(p => new ProjectHeader { ProjectId = p.ProjectId, ProjectName = p.ProjectName });
    }
}
