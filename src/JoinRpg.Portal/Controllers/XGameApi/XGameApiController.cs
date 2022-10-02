using JoinRpg.Data.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Web.Controllers.XGameApi;

[ApiController]
[IgnoreAntiforgeryToken] //It's not required, because this is not browser-based API.
public class XGameApiController : ControllerBase
{
    public XGameApiController(IProjectRepository projectRepository) => ProjectRepository = projectRepository;

    public IProjectRepository ProjectRepository { get; }
}
