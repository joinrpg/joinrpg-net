using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.CommonTypes;

namespace JoinRpg.Web.Controllers.Common
{
  [Authorize]
  public class PrintController : ControllerGameBase
  {
    public PrintController(ApplicationUserManager userManager, IProjectRepository projectRepository, IProjectService projectService, IExportDataService exportDataService) : base(userManager, projectRepository, projectService, exportDataService)
    {
    }


    public async Task<ActionResult> Character(int projectid, int characterid)
    {
      var field = await ProjectRepository.GetCharacterWithGroups(projectid, characterid);
      var error = WithEntity(field);
      if (error != null) return error;
      return View(new PrintCharacterViewModel()
      {
        CharacterName = field.CharacterName,
        Player = field.ApprovedClaim?.Player,
        FeeDue = field.ApprovedClaim?.ClaimFeeDue() ?? field.Project.CurrentFee(),
        ProjectName = field.Project.ProjectName,
      });
    }
  }
}