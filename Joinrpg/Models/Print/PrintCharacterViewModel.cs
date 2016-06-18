using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Web.Models.Characters;
using JoinRpg.Web.Models.CommonTypes;
using JoinRpg.Web.Models.Plot;

namespace JoinRpg.Web.Models.Print
{
  public class PrintCharacterViewModelSlim
  {
    public string ProjectName { get;  }
    public string CharacterName { get;  }
    public int FeeDue { get; }
    public IReadOnlyCollection<CharacterGroupWithDescViewModel> Groups { get; }
    [CanBeNull]
    public User ResponsibleMaster { get; }
    public string PlayerDisplayName { get; }
    public string PlayerFullName { get; }

    public PrintCharacterViewModelSlim(Character character)
    {
      CharacterName = character.CharacterName;
      FeeDue = character.ApprovedClaim?.ClaimFeeDue() ?? character.Project.CurrentFee();
      ProjectName = character.Project.ProjectName;

      Groups =
        character.GetParentGroupsToTop()
          .Where(g => !g.IsSpecial && g.IsActive && g.IsPublic && !g.IsRoot)
          .Distinct()
          .Select(g => new CharacterGroupWithDescViewModel(g))
          .ToArray();
      ResponsibleMaster = character.GetResponsibleMaster();
      PlayerDisplayName = character.ApprovedClaim?.Player.DisplayName;
      PlayerFullName = character.ApprovedClaim?.Player.FullName;
    }
  }

  public class PrintCharacterViewModel : PrintCharacterViewModelSlim
  {
    public MarkdownViewModel CharacterDescription{ get; }
    public IReadOnlyCollection<PlotElementViewModel> Plots { get; }
    public IReadOnlyCollection<MarkdownViewModel> Handouts { get; }
    public string PlayerPhoneNumber { get; }
    public CustomFieldsViewModel Fields { get; }
    public bool RegistrationOnHold => FeeDue > 0 && Plots.Any(item => item.Status == PlotStatus.InWork);

    public PrintCharacterViewModel (int currentUserId, Character character, IReadOnlyCollection<PlotElement> plots)
      : base(character)
    {
      
      CharacterDescription = new MarkdownViewModel(character.Description);
      
      var plotElements = character.GetOrderedPlots(character.SelectPlots(plots)).ToArray();
      Plots =
        plotElements
          .ToViewModels(character.HasMasterAccess(currentUserId), character.CharacterId)
          .ToArray();

      Handouts =
        plotElements.Where(e => e.ElementType == PlotElementType.Handout)
          .Select(e => new MarkdownViewModel(e.Texts.Content))
          .ToArray();
      
      
      PlayerPhoneNumber = character.ApprovedClaim?.Player.Extra?.PhoneNumber;
      Fields = new CustomFieldsViewModel(currentUserId, character, disableEdit: true, onlyPlayerVisible: true);
    }
  }
}