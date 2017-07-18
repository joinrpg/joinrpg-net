using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using JoinRpg.Data.Interfaces;

namespace JoinRpg.Web.Controllers.XGameApi
{
  [RoutePrefix("x-api/me")]
  public class MyProfileController : XGameApiController
  {
    public MyProfileController(IProjectRepository projectRepository) : base(projectRepository)
    {
    }

    [HttpGet, Authorize, Route("projects/active")]
    public async Task<object> GetActiveProjects()
    {
      return (await ProjectRepository.GetMyActiveProjectsAsync(GetCurrentUserId())).Select(p => new
      {
        p.ProjectId,
        p.ProjectName
      });
    }
  }
}