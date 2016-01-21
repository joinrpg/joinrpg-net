using System.Data.Entity.Validation;
using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models;

namespace JoinRpg.Web.Controllers
{
  [Authorize]
  public class CommentController : Common.ControllerGameBase
  {
    private IClaimService ClaimService { get; }

    public CommentController(ApplicationUserManager userManager, IProjectRepository projectRepository,
      IProjectService projectService, IClaimService claimService, IExportDataService exportDataService)
      : base(userManager, projectRepository, projectService, exportDataService)
    {
      ClaimService = claimService;
    }

    [HttpPost]
   [ValidateAntiForgeryToken]
    public async Task<ActionResult> Create(AddCommentViewModel viewModel)
    {
      var claim = await ProjectRepository.GetClaim(viewModel.ProjectId, viewModel.ClaimId);

      var error = WithClaim(claim);
      if (error != null) return error;
      try
      {
        if (viewModel.HideFromUser)
        {
          claim.Project.RequestMasterAccess(CurrentUserId);
        }
        await ClaimService.AddComment(claim.ProjectId, claim.ClaimId, CurrentUserId, viewModel.ParentCommentId,
          !viewModel.HideFromUser, viewModel.CommentText.Contents, viewModel.FinanceAction);

        return RedirectToAction("Edit", "Claim", new {viewModel.ClaimId, viewModel.ProjectId});
      }
      catch
      {
        //TODO: Message that comment is not added
        return RedirectToAction("Edit", "Claim", new {viewModel.ClaimId, viewModel.ProjectId});
      }

    }
  }
}