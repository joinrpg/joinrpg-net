using JoinRpg.Data.Interfaces;
using JoinRpg.Markdown;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Web.XGameApi.Contract;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Web.Controllers.XGameApi;

[Route("x-game-api/{projectId}/metadata"), XGameMasterAuthorize]
public class MetaDataApiController : XGameApiController
{
    private readonly IProjectMetadataRepository projectMetadataRepository;

    public MetaDataApiController(IProjectRepository projectRepository, IProjectMetadataRepository projectMetadataRepository) : base(projectRepository)
    {
        this.projectMetadataRepository = projectMetadataRepository;
    }

    /// <summary>
    /// All required metadata for fields
    /// </summary>
    [HttpGet]
    [Route("fields")]
    public async Task<ProjectFieldsMetadata> GetFieldsList(int projectId)
    {
        var project = await projectMetadataRepository.GetProjectMetadata(new(projectId));
        return
            new ProjectFieldsMetadata
            {
                ProjectId = project.ProjectId,
                ProjectName = project.ProjectName,
                Fields = project.SortedFields.Select(field =>
                    new ProjectFieldInfo
                    {
                        FieldName = field.Name,
                        ProjectFieldId = field.Id.ProjectFieldId,
                        IsActive = field.IsActive,
                        FieldType = field.Type.ToString(),
                        ProgrammaticValue = field.ProgrammaticValue,
                        ValueList = field.SortedVariants.Select(variant =>
                            new ProjectFieldVariant
                            {
                                ProjectFieldVariantId = variant.Id.ProjectFieldVariantId,
                                Label = variant.Label,
                                IsActive = variant.IsActive,
                                Description = variant.Description.ToHtmlString().ToHtmlString(),
                                MasterDescription =
                                    variant.MasterDescription.ToHtmlString().ToHtmlString(),
                                ProgrammaticValue = variant.ProgrammaticValue,
                            }),
                    }),
            };
    }
}
