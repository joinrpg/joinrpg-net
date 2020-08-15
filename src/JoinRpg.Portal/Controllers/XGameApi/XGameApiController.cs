using JoinRpg.Data.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Web.Controllers.XGameApi
{
    [ApiController]
    public class XGameApiController : ControllerBase
    {
        public XGameApiController(IProjectRepository projectRepository) => ProjectRepository = projectRepository;

        public IProjectRepository ProjectRepository { get; }
    }
}
