using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.Plot;

namespace JoinRpg.Web.Controllers.Common
{
  [Authorize]
  public class PrintController : ControllerGameBase
  {
    private IPlotRepository PlotRepository { get; }

    public PrintController(ApplicationUserManager userManager, IProjectRepository projectRepository, IProjectService projectService, IExportDataService exportDataService, IPlotRepository plotRepository) : base(userManager, projectRepository, projectService, exportDataService)
    {
      PlotRepository = plotRepository;
    }


    public async Task<ActionResult> Character(int projectid, int characterid)
    {
      var character = await ProjectRepository.GetCharacterWithGroups(projectid, characterid);
      var error = WithCharacter(character);
      if (error != null) return error;

      return View(new PrintCharacterViewModel()
      {
        CharacterName = character.CharacterName,
        Player = character.ApprovedClaim?.Player,
        FeeDue = character.ApprovedClaim?.ClaimFeeDue() ?? character.Project.CurrentFee(),
        ProjectName = character.Project.ProjectName,
        Plots =
          character.GetOrderedPlots(await PlotRepository.GetPlotsForCharacter(character))
            .ToViewModels(character.HasMasterAccess(CurrentUserId))
            .ToArray(),
        Groups = character.GetParentGroups().Where(g => !g.IsSpecial && g.IsActive).Select(g => new CharacterGroupWithDescViewModel(g)).ToArray(),
        ResponsibleMaster = character.ApprovedClaim?.ResponsibleMasterUser,
        PlayerDetails = UserProfileDetailsViewModel.FromUser(character.ApprovedClaim?.Player, GetCurrentUser()),
        Fields = new CustomFieldsViewModel(CurrentUserId, character, onlyPlayerVisible: true)
      });
    }
  }
}