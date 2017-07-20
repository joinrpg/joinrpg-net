using System.Collections.Generic;
using System.Linq;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;

namespace JoinRpg.Web.Models.CheckIn
{
  public class CheckInIndexViewModel : IProjectIdAware
  {
    public CheckInIndexViewModel(Project project, IReadOnlyCollection<ClaimWithPlayer> claims)
    {
      ProjectId = project.ProjectId;
      Claims = claims.Select(claim => new CheckInListItemViewModel(claim)).ToList();
    }

    public int ProjectId { get; }

    public IReadOnlyCollection<CheckInListItemViewModel> Claims { get; }
  }

  public class CheckInListItemViewModel
  {
    

    public CheckInListItemViewModel(ClaimWithPlayer claim)
    {
      ClaimId = claim.ClaimId;
      CharacterName = claim.CharacterName;
      Fullname = claim.Player.FullName;
      NickName = claim.Player.DisplayName;
      OtherNicks = claim.Player.Extra?.Nicknames ?? "";
    }

    public string OtherNicks { get;  }

    public string NickName { get; }

    public string Fullname { get; }

    public string CharacterName { get; }

    public int ClaimId { get; }
  }
}