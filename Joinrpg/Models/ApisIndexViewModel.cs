using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Helpers;

namespace JoinRpg.Web.Models
{
  public class ApisIndexViewModel
  {
    public int ProjectId { get; }
    public int RootGroupId { get; }
    public string CurrentUserToken { get; }

    public ApisIndexViewModel(Project project, int currentUserId)
    {
      ProjectId = project.ProjectId;
      RootGroupId = project.RootGroup.CharacterGroupId;
      CurrentUserToken = project.ProjectAcls.Single(acl => acl.UserId == currentUserId).Token.ToHexString();
    }
  }
}