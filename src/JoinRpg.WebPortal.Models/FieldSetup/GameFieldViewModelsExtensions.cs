namespace JoinRpg.Web.Models.FieldSetup;

public static class GameFieldViewModelsExtensions
{
    public static IEnumerable<GameFieldEditViewModel> ToViewModels(
        this IEnumerable<ProjectFieldInfo> fields,
        ProjectInfo projectInfo,
        UserIdentification userId)
        => fields.Select(f => new GameFieldEditViewModel(f, projectInfo, userId)).MarkFirstAndLast();
}
