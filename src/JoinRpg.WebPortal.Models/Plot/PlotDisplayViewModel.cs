using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.Access;
using JoinRpg.Interfaces;
using JoinRpg.Markdown;
using JoinRpg.Web.Plots.Elements;

namespace JoinRpg.Web.Models.Plot;

public class PlotDisplayViewModel
{
    /// <param name="linkRenderer">
    /// Разворачивает директивы (<c>%персонаж</c>, <c>%список</c>) в тексте вводных. Собирается
    /// вызывающим через <c>JoinrpgMarkdownLinkRendererFactory</c>: кто рендерит текст, тот и платит
    /// за загрузку данных. Когда вводных нет, сюда приходит
    /// <c>JoinrpgMarkdownLinkRendererFactory.NoDirectives</c>.
    /// </param>
    public PlotDisplayViewModel(IReadOnlyCollection<PlotTextDto> plots,
        ICurrentUserAccessor currentUser,
        Character character,
        ILinkRenderer linkRenderer,
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

        Elements = plots.Select(p => p.Render(linkRenderer, projectInfo, currentUser)).ToList();
    }

    // Blazor-параметру PlotElementsView.PlotTexts нужен конкретный публичный тип (List<T>),
    // а не внутренний тип, который генерирует компилятор для collection expression с target-type IReadOnlyList<T> —
    // иначе десериализация параметра при WebAssemblyPrerendered падает с "type could not be found".
    public List<PlotRenderedTextViewModel> Elements { get; }

    public CharacterIdentification CharacterId { get; }

    public bool ShowEditControls { get; }
}
