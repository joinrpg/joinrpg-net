using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.Web.Helpers;

namespace JoinRpg.Web.Areas.Admin.Controllers
{
    [AdminAuthorize]
    public class AdminHomeController : Web.Controllers.Common.ControllerBase
    {
        private IProjectRepository ProjectRepository { get; }
        

        public AdminHomeController(ApplicationUserManager userManager,
            IUserRepository userRepository, IProjectRepository projectRepository) : base(userRepository)
        {
            ProjectRepository = projectRepository;
        }

        public ActionResult Index() => View();

        public async Task<ActionResult> StaleGames()
        {
            var projects = await ProjectRepository.GetStaleProjects(DateTime.Now.AddMonths(-4));
            return View(projects);
        }
    }
}
