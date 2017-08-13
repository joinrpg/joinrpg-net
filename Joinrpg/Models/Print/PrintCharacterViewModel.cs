using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using JetBrains.Annotations;
using Joinrpg.Markdown;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Web.Models.Characters;
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
    public IHtmlString CharacterDescription{ get; }
    public PlotDisplayViewModel Plots { get; }
    public IReadOnlyCollection<HandoutListItemViewModel> Handouts { get; }
    public string PlayerPhoneNumber { get; }
    public CustomFieldsViewModel Fields { get; }
    public bool RegistrationOnHold => FeeDue > 0 && Plots.HasUnready;

    public PrintCharacterViewModel 
      (int currentUserId, [NotNull] Character character, IReadOnlyCollection<PlotElement> plots)
      : base(character)
    {
      if (character == null) throw new ArgumentNullException(nameof(character));
      CharacterDescription = character.Description.ToHtmlString();
      
      var plotElements = character.GetOrderedPlots(character.SelectPlots(plots)).ToArray();
      Plots = PlotDisplayViewModel.Published(plotElements, currentUserId, character);

      Handouts =
        plotElements.Where(e => e.ElementType == PlotElementType.Handout && e.IsActive)
          .Select(e => e.PublishedVersion())
          .Where(e => e != null)
          .Select(e => new HandoutListItemViewModel(e.Content.ToPlainText(), e.AuthorUser))
          .ToArray();
      
      PlayerPhoneNumber = character.ApprovedClaim?.Player.Extra?.PhoneNumber;
      Fields = new CustomFieldsViewModel(currentUserId, character, disableEdit: true, onlyPlayerVisible: true, wherePrintEnabled: true);
    }
  }

  public class HandoutListItemViewModel : HandoutViewModelBase
  {
    public HandoutListItemViewModel(IHtmlString text, User master) : base(text, master)
    {
    }
  }
}