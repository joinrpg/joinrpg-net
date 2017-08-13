using System.Web.Http;
using JoinRpg.Data.Interfaces;
using Microsoft.AspNet.Identity;

namespace JoinRpg.Web.Controllers.XGameApi
{
  public class XGameApiController : ApiController
  {
    public XGameApiController(IProjectRepository projectRepository)
    {
      ProjectRepository = projectRepository;
    }

    public IProjectRepository ProjectRepository { get; }

    protected int GetCurrentUserId()
    {
      return int.Parse(User.Identity.GetUserId());
    }
  }
}