using System.Web.Http;
using JoinRpg.Data.Interfaces;

namespace JoinRpg.Web.Controllers.XGameApi
{
  public class XGameApiController : ApiController
  {
    public XGameApiController(IProjectRepository projectRepository)
    {
      ProjectRepository = projectRepository;
    }

    public IProjectRepository ProjectRepository { get; }
  }
}