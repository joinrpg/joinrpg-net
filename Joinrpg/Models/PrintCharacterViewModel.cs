using System.Collections.Generic;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Web.Models.Plot;

namespace JoinRpg.Web.Models
{
  public class PrintCharacterViewModel
  {
    public string ProjectName { get;  }
    public string CharacterName { get;  }
    public int FeeDue { get; }
    public bool RegistrationOnHold => FeeDue > 0 && Plots.Any(item => item.Status == PlotStatus.InWork);
    public IReadOnlyCollection<PlotElementViewModel> Plots { get; }
    public IReadOnlyCollection<CharacterGroupWithDescViewModel> Groups { get; }
    public User ResponsibleMaster { get; set; }
    public UserProfileDetailsViewModel PlayerDetails { get; set; }
    public CustomFieldsViewModel Fields { get; }

    public PrintCharacterViewModel (int currentUserId, Character character, IReadOnlyCollection<PlotElement> plots)
    {
      CharacterName = character.CharacterName;
      FeeDue = character.ApprovedClaim?.ClaimFeeDue() ?? character.Project.CurrentFee();
      ProjectName = character.Project.ProjectName;
      Plots =
        character.GetOrderedPlots(plots.Where(character.ShouldShowPlot).ToArray())
          .ToViewModels(character.HasMasterAccess(currentUserId))
          .ToArray();
      Groups =
        character.GetParentGroups()
          .Where(g => !g.IsSpecial && g.IsActive)
          .Select(g => new CharacterGroupWithDescViewModel(g))
          .ToArray();
      ResponsibleMaster = character.ApprovedClaim?.ResponsibleMasterUser;
      PlayerDetails = UserProfileDetailsViewModel.FromUser(character.ApprovedClaim?.Player);
      Fields = new CustomFieldsViewModel(currentUserId, character, onlyPlayerVisible: true);
    }
  }
}