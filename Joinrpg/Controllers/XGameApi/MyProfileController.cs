using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using JoinRpg.Data.Interfaces;
using JoinRpg.Web.XGameApi.Contract;

namespace JoinRpg.Web.Controllers.XGameApi
{
  [RoutePrefix("x-api/me")]
  public class MyProfileController : XGameApiController
  {
    public MyProfileController(IProjectRepository projectRepository) : base(projectRepository)
    {
    }

    [HttpGet, Authorize, Route("projects/active")]
    public async Task<IEnumerable<ProjectHeader>> GetActiveProjects()
    {
      return (await ProjectRepository.GetMyActiveProjectsAsync(GetCurrentUserId())).Select(
        p => new ProjectHeader {ProjectId = p.ProjectId, ProjectName = p.ProjectName});
    }
  }
}