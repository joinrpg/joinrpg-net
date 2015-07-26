using System.Data.Entity.Validation;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models;

namespace JoinRpg.Web.Controllers
{
  public class CommentController : Common.ControllerGameBase
  {
    private IClaimService ClaimService
    { get; }
    public CommentController(ApplicationUserManager userManager, IProjectRepository projectRepository, IProjectService projectService, IClaimService claimService) : base(userManager, projectRepository, projectService)
    {
      ClaimService = claimService;
    }

  //  [HttpPost]
    [Authorize]
//    [ValidateAntiForgeryToken]
    public ActionResult Create(AddCommentViewModel viewModel)
    {
      return WithClaim(viewModel.ProjectId, viewModel.ClaimId, (project, claim, hasMasterAccess, isMyClaim) =>
      {
        try
        {
          if (viewModel.HideFromUser && !hasMasterAccess)
          {
            throw new DbEntityValidationException();
          }
          ClaimService.AddComment(project.ProjectId, claim.ClaimId, CurrentUserId, viewModel.ParentCommentId, !(viewModel.HideFromUser),
            isMyClaim, viewModel.CommentText);

          return RedirectToAction("Edit", "Claim", new {viewModel.ClaimId, viewModel.ProjectId});
        }
        catch
        {
          //TODO: Message that comment is not added
          return RedirectToAction("Edit", "Claim", new { viewModel.ClaimId, viewModel.ProjectId });
        }
      });
    }
  }
}