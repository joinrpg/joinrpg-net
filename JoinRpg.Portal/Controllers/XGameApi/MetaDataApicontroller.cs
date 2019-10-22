using System.Linq;
using System.Threading.Tasks;
using Joinrpg.Markdown;
using JoinRpg.Data.Interfaces;
using JoinRpg.Domain;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Web.XGameApi.Contract;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Web.Controllers.XGameApi
{
    [Route("x-game-api/{projectId}/metadata"), XGameMasterAuthorize()]
    public class MetaDataApiController : XGameApiController
    {
        public MetaDataApiController(IProjectRepository projectRepository) : base(projectRepository)
        {
        }

        /// <summary>
        /// All required metadata for fields
        /// </summary>
        [HttpGet]
        [Route("fields")]
        public async Task<ProjectFieldsMetadata> GetFieldsList(int projectId)
        {
            var project = await ProjectRepository.GetProjectWithFieldsAsync(projectId);
            return
                new ProjectFieldsMetadata
                {
                    ProjectId = project.ProjectId,
                    ProjectName = project.ProjectName,
                    Fields = project.GetOrderedFields().Select(field =>
                        new ProjectFieldInfo
                        {
                            FieldName = field.FieldName,
                            ProjectFieldId = field.ProjectFieldId,
                            IsActive = field.IsActive,
                            FieldType = field.FieldType.ToString(),
                            ProgrammaticValue = field.ProgrammaticValue,
                            ValueList = field.GetOrderedValues().Select(variant =>
                                new ProjectFieldVariant
                                {
                                    ProjectFieldVariantId = variant.ProjectFieldDropdownValueId,
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
}
