using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.Access;
using JoinRpg.Interfaces;
using JoinRpg.Web.Models.Helpers;
using JoinRpg.Web.Plots;

namespace JoinRpg.Web.Models.Plot;

public class PlotDisplayViewModel
{
    /// <param name="projectForRendering">
    /// EF-граф проекта, поверх которого рендерер разворачивает директивы (<c>%персонаж</c>,
    /// <c>%список</c>). Граф передаётся отдельным параметром, а не берётся из
    /// <c>character.Project</c>: иначе его состав зависит от того, что случайно загрузил вызывающий,
    /// и EF6 дотягивает персонажей с заявками по одному (#4992). Кто рендерит текст, тот и грузит
    /// граф — <c>IProjectRepository.GetProjectForMarkdownRendering</c>.
    /// </param>
    public PlotDisplayViewModel(IReadOnlyCollection<PlotTextDto> plots,
        ICurrentUserAccessor currentUser,
        Character character,
        Project projectForRendering,
        ProjectInfo projectInfo
        )
    {
        ArgumentNullException.ThrowIfNull(plots);

        var accessArguments = AccessArgumentsFactory.Create(character, currentUser, projectInfo);

        CharacterId = character.GetId();
        ShowEditControls = accessArguments.MasterAccess && accessArguments.EditAllowed;

        if (plots.Count == 0 || !accessArguments.CharacterPlotAccess)
        {
            Elements = [];
            return;
        }

        var linkRenderer = new JoinrpgMarkdownLinkRenderer(projectForRendering, projectInfo);

        Elements = plots.Select(p => p.Render(linkRenderer, projectInfo, currentUser)).ToList();
    }

    // Blazor-параметру PlotElementsView.PlotTexts нужен конкретный публичный тип (List<T>),
    // а не внутренний тип, который генерирует компилятор для collection expression с target-type IReadOnlyList<T> —
    // иначе десериализация параметра при WebAssemblyPrerendered падает с "type could not be found".
    public List<PlotRenderedTextViewModel> Elements { get; }

    public CharacterIdentification CharacterId { get; }

    public bool ShowEditControls { get; }
}
