using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models;

namespace JoinRpg.Web.Areas.Admin.Controllers
{
    [AdminAuthorize]
    public class ProjectListController : Web.Controllers.Common.ControllerBase
    {
        private readonly IProjectRepository _projectRepository;

        public async Task<ActionResult> Index()
        {
            var allProjects =  await _projectRepository.GetActiveProjectsWithClaimCount(CurrentUserIdOrDefault);

            var projects =
                allProjects
                    .Select(p => new ProjectListItemViewModel(p))
                    .OrderByDescending(p => p.ClaimCount)
                    .ToList();

            return View(projects);
        }

        public ProjectListController(ApplicationUserManager userManager, IProjectRepository projectRepository) : base(userManager)
        {
            _projectRepository = projectRepository;
        }
    }
}
