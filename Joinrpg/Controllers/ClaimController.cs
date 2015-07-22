using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Controllers.Common;
using JoinRpg.Web.Models;

namespace JoinRpg.Web.Controllers
{
  public class ClaimController : ControllerGameBase
  {
    private readonly IClaimService _claimService;

    [HttpGet]
    [Authorize]
    public ActionResult AddForCharacter(int projectid, int characterid)
    {
      return WithCharacter(projectid, characterid, (project, character) => View("Add", AddClaimViewModel.Create(character, GetCurrentUser())));
    }

    [HttpGet]
    [Authorize]
    public ActionResult AddForGroup(int projectid, int characterGroupId)
    {
      return WithGroup(projectid, characterGroupId, (project, group) => View("Add", AddClaimViewModel.Create(group, GetCurrentUser())));
    }

    public ClaimController(ApplicationUserManager userManager, IProjectRepository projectRepository, IProjectService projectService, IClaimService claimService) : base(userManager, projectRepository, projectService)
    {
      _claimService = claimService;
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public ActionResult Add(AddClaimViewModel viewModel)
    {
      return WithProject(viewModel.ProjectId, project =>
      {
        try
        {
          _claimService.AddClaimFromUser(viewModel.ProjectId, viewModel.CharacterGroupId, viewModel.CharacterId,
            CurrentUserId, viewModel.ClaimText);

          return RedirectToAction("My", "Claim");
        }
        catch
        {
          return View(viewModel);
        }
      });
    }

    [HttpGet]
    [Authorize]
    public ActionResult My()
    {
      return View(GetCurrentUser().Claims);
    }
  }
}