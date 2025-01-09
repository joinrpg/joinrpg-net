using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.WebComponents;

namespace JoinRpg.Web.ProjectCommon;
public static class UserLinkExtensions
{
    public static UserLinkViewModel ToUserLinkViewModel(this ProjectMasterInfo projectMasterInfo) =>
        new UserLinkViewModel(projectMasterInfo.UserId, projectMasterInfo.Name.DisplayName, ViewMode.Show);
}
