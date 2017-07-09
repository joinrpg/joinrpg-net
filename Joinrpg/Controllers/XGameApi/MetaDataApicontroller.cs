using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using JoinRpg.Data.Interfaces;
using JoinRpg.Domain;
using JoinRpg.Web.Filter;

namespace JoinRpg.Web.Controllers.XGameApi
{
  [RoutePrefix("x-game-api/{projectId}/metadata"), XGameAuthorize()]
  public class MetaDataApiController : XGameApiController
  {
    public MetaDataApiController(IProjectRepository projectRepository) : base(projectRepository)
    {
    }

    [Route("fields")]
    public async Task<object> GetFieldsList(int projectId)
    {
      var project = await ProjectRepository.GetProjectAsync(projectId);
      return
        new
        {
          project.ProjectId,
          project.ProjectName,
          Fields = project.GetOrderedFields().Select(field =>
           new
           {
             field.FieldName,
             field.ProjectFieldId,
             field.IsActive,
             FieldType = field.FieldType.ToString(),
             ValueList = field.GetOrderedValues().Select(variant =>
             new {
               variant.ProjectId,
               variant.Label,
               variant.IsActive
             })
           }
          )
        };
    }
  }
}