using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Web.Models.Characters;

namespace JoinRpg.Web.Models.CharacterGroups
{
  public enum GroupNavigationPage
  {
    Home,
    Roles,
    ClaimsActive,
    ClaimsDiscussing,
    ClaimsDirect,
    Characters,
    Report
  }

  public class CharacterGroupDetailsViewModel : CharacterGroupWithDescViewModel
  {
    public GroupNavigationPage Page { get; }

    public CharacterGroupDetailsViewModel(CharacterGroup group, int? currentUser, GroupNavigationPage page) : base(group)
    {
      Page = page;
      HasMasterAccess = group.HasMasterAccess(currentUser);
      ShowEditControls = group.HasEditRolesAccess(currentUser);
      IsSpecial = group.IsSpecial;
      AvaiableDirectSlots = group.HaveDirectSlots ? group.AvaiableDirectSlots : 0;
      IsAcceptingClaims = group.IsAcceptingClaims();
      ActiveClaimsCount = group.Claims.Count(c => c.IsActive);
      IsRootGroup = group.IsRoot;
    }

    public bool HasMasterAccess { get; private set; }
    public bool ShowEditControls { get; private set; }
    public bool IsSpecial { get; private set; }
    public int AvaiableDirectSlots { get; private set; }
    public int ActiveClaimsCount { get; private set; }
    public bool IsAcceptingClaims { get; private set; }
    public bool IsRootGroup { get; private set; }
  }
}