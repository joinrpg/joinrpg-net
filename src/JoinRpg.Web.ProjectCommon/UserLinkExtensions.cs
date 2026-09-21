using System.Diagnostics.CodeAnalysis;
using JoinRpg.Common.PrimitiveTypes.Users;

namespace JoinRpg.Web.ProjectCommon;

public static class UserLinkExtensions
{
    /// <summary>
    /// Обёртка над конструктором <see cref="UserLinkViewModel"/> для случая, когда пользователя может не быть.
    /// </summary>
    [return: NotNullIfNotNull(nameof(user))]
    public static UserLinkViewModel? ToUserLinkViewModel(this UserInfoHeader? user, ViewMode viewMode = ViewMode.Show) =>
        user is null ? null : new UserLinkViewModel(user, viewMode);

    public static CreateUpdateMarksViewModel ToViewModel(this CreateUpdateMarksInfo marks) =>
        new(marks.CreatedAt, marks.CreatedBy.ToUserLinkViewModel(), marks.UpdatedAt, marks.UpdatedBy.ToUserLinkViewModel());
}
