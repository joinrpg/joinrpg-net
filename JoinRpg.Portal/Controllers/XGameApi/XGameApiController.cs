using JoinRpg.Data.Interfaces;
using Microsoft.AspNet.Identity;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Web.Controllers.XGameApi
{
    [ApiController]
  public class XGameApiController : ControllerBase
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
