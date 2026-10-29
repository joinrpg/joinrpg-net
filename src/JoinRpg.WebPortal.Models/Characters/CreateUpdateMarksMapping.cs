using JoinRpg.Common.WebComponents;
using JoinRpg.DataModel;
using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.Web.Models.Characters;

internal static class CreateUpdateMarksMapping
{
    /// <summary>
    /// Отметки «создано/изменено» для общего компонента <c>CreateUpdatedMarks</c> поверх уже
    /// загруженной EF-сущности.
    /// </summary>
    public static CreateUpdateMarksViewModel ToCreateUpdateMarksViewModel(this ICreatedUpdatedTrackedForEntity entity)
        => new(entity.CreatedAt, ToUserLink(entity.CreatedBy), entity.UpdatedAt, ToUserLink(entity.UpdatedBy));

    // Автора может не оказаться, как и в отметках групп: компонент такой случай рисует без ссылки.
    private static UserLinkViewModel? ToUserLink(User? user)
        => user?.ToUserInfoHeader().ToUserLinkViewModel();
}
