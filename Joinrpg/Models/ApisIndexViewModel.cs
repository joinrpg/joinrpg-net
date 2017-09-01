using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Helpers;

namespace JoinRpg.Web.Models
{
  public class ApisIndexViewModel
  {
    public int ProjectId { get; }
    public int RootGroupId { get; }

    public ApisIndexViewModel(Project project, int currentUserId)
    {
      ProjectId = project.ProjectId;
      RootGroupId = project.RootGroup.CharacterGroupId;
      project.ProjectAcls.Single(acl => acl.UserId == currentUserId).Token.ToHexString();
    }
  }
}