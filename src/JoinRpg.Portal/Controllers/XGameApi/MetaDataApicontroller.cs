using JoinRpg.Data.Interfaces;
using JoinRpg.Markdown;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.XGameApi.Contract;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.XGameApi;

[Route("x-game-api/{projectId}/metadata"), XGameMasterAuthorize]
public class MetaDataApiController(IProjectMetadataRepository projectMetadataRepository) : XGameApiController
{

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
                                Description = variant.Description.ToHtmlString().Value,
                                MasterDescription =
                                    variant.MasterDescription.ToHtmlString().Value,
                                ProgrammaticValue = variant.ProgrammaticValue,
                            }),
                    }),
            };
    }
}
