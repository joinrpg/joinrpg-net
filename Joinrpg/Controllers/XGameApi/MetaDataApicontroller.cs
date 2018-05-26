using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using Joinrpg.Markdown;
using JoinRpg.Data.Interfaces;
using JoinRpg.Domain;
using JoinRpg.Web.Filter;
using JoinRpg.Web.XGameApi.Contract;

namespace JoinRpg.Web.Controllers.XGameApi
{
  [RoutePrefix("x-game-api/{projectId}/metadata"), XGameAuthorize()]
  public class MetaDataApiController : XGameApiController
  {
    public MetaDataApiController(IProjectRepository projectRepository) : base(projectRepository)
    {
    }

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
              ValueList = field.GetOrderedValues().Select(variant =>
                new ProjectFieldVariant
                {
                  ProjectFieldVariantId = variant.ProjectFieldDropdownValueId,
                  Label = variant.Label,
                  IsActive = variant.IsActive,
                  Description = variant.Description.ToHtmlString().ToHtmlString(),
                  MasterDescription = variant.MasterDescription.ToHtmlString().ToHtmlString(),
                  ProgrammaticValue = variant.ProgrammaticValue,
                }),
            }),
        };
    }
  }
}