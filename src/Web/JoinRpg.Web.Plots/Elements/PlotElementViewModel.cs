using JoinRpg.DomainTypes.ProjectMetadata;
using static JoinRpg.Web.Plots.PlotStatus;

namespace JoinRpg.Web.Plots.Elements;

// Content хранится как string, а не MarkupString: MarkupString не переживает
// JSON-сериализацию параметров компонента на границе WebAssemblyPrerendered
// (нет публичного конструктора для десериализации) — текст просто пропадает при гидратации.
public record class PlotRenderedTextViewModel(
    string Content,
    string ShortContent,
    string? Todo,
    PlotVersionIdentification PlotVersionId,
    PlotStatus? PlotStatus,
    TargetsInfo? Target,
    PlotElementTypeView ElementType,
    ProjectRolesListIdentification? RolesListId,
    string? RolesListName)
{
    public bool HasWorkTodo => !string.IsNullOrWhiteSpace(Todo) || PlotStatus == InWork || PlotStatus == HasNewVersion;

    /// <summary>Подпись вводной-сетки: заголовок у персонажа, строка в диалоге перестановки, печать.</summary>
    public static string FormatRoleGridCaption(string? rolesListName) => $"Сетка ролей: {rolesListName ?? DeletedRolesListName}";

    public const string DeletedRolesListName = "сетка удалена";
}
