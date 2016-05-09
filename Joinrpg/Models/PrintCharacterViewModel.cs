using System.Collections.Generic;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Web.Models.Plot;

namespace JoinRpg.Web.Models
{
  public class PrintCharacterViewModel
  {
    public string ProjectName { get; set; }
    public string CharacterName { get; set; }
    public User Player { get; set; }
    public int FeeDue { get; set; }
    public bool RegistrationOnHold => FeeDue > 0 && Plots.Any(item => item.Status == PlotStatus.InWork);
    public IReadOnlyCollection<PlotElementViewModel> Plots { get; set; }
    public IReadOnlyCollection<CharacterGroupWithDescViewModel> Groups { get; set; }
    public User ResponsibleMaster { get; set; }
    public UserProfileDetailsViewModel PlayerDetails { get; set; }
    public CustomFieldsViewModel Fields { get; set; }
  }
}