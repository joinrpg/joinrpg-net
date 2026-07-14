using JoinRpg.Markdown;
using JoinRpg.Web.Games.FieldSetup;
using JoinRpg.Web.ProjectCommon.Fields;

namespace JoinRpg.WebPortal.Managers.Fields;

internal static class GameFieldViewModelsBuilder
{
    extension(IEnumerable<ProjectFieldInfo> fields)
    {
        public IList<GameFieldListItemViewModel> ToViewModels(
        ProjectInfo projectInfo,
        UserIdentification userId)
        => fields.Select(f => ToListItemViewModel(f, projectInfo, userId)).ToArray().MarkFirstAndLast();
    }

    private static GameFieldListItemViewModel ToListItemViewModel(
        ProjectFieldInfo field,
        ProjectInfo projectInfo,
        UserIdentification userId)
    {
        var canEditFields = projectInfo.HasMasterAccess(userId, Permission.CanChangeFields);
        return new GameFieldListItemViewModel
        {
            Id = field.Id,
            Name = field.Name,
            IsActive = field.IsActive,
            IsPublic = field.IsPublic,
            CanPlayerView = field.CanPlayerView,
            CanPlayerEdit = field.CanPlayerEdit,
            GroupNames = [.. projectInfo.GetGroupsById(field.GroupsAvailableForIds).Select(g => g.Name)],
            MandatoryStatus = (MandatoryStatusViewType)field.MandatoryStatus,
            FieldViewType = (ProjectFieldViewType)field.Type,
            FieldBoundTo = (FieldBoundToViewModel)field.BoundTo,
            Price = field.Price,
            DescriptionHtml = field.Description?.ToHtmlString().Value,
            MasterDescriptionHtml = field.MasterDescription?.ToHtmlString().Value,
            HasValueList = field.HasValueList,
            Variants = [.. field.SortedVariants.Select(v => new GameFieldVariantDisplayItem(
                v.Label,
                v.IsActive,
                v.CharacterGroupId))],
            WasEverUsed = field.WasEverUsed,
            CanEditFields = canEditFields,
        };
    }
}
