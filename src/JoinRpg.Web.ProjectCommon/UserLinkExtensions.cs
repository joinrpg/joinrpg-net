using System.Diagnostics.CodeAnalysis;
using JoinRpg.Common.PrimitiveTypes.Users;

namespace JoinRpg.Web.ProjectCommon;

public static class UserLinkExtensions
{
    public static UserLinkViewModel ToUserLinkViewModel(this ProjectMasterInfo projectMasterInfo) =>
        new UserLinkViewModel(projectMasterInfo.UserId, projectMasterInfo.Name.DisplayName, ViewMode.Show);

    [return: NotNullIfNotNull(nameof(user))]
    public static UserLinkViewModel? ToUserLinkViewModel(this UserInfoHeader? user) =>
        user is null ? null : new UserLinkViewModel(user.UserId.Value, user.DisplayName.DisplayName, ViewMode.Show);

    public static CreateUpdateMarksViewModel ToViewModel(this CreateUpdateMarksInfo marks) =>
        new(marks.CreatedAt, marks.CreatedBy.ToUserLinkViewModel(), marks.UpdatedAt, marks.UpdatedBy.ToUserLinkViewModel());
}
