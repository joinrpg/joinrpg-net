using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Plots;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Web.Plots.Folders;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Web.Plots.Test;

public class PlotFolderListItemRowTest : BunitContext
{
    private static readonly ProjectIdentification ProjectId = new(1);

    /// <summary>
    /// Между тегами сюжета — пробел, а не склейка «#лор#бой»: статическую разметку из одних
    /// пробелов (включая <text> </text>) Blazor вырезает (#5324).
    /// </summary>
    [Fact]
    public void TagsAreSeparatedWithSpaces()
    {
        Services.AddSingleton<IUriLocator<PlotFolderIdentification>>(new FakeFolderLocator());
        // Без прав мастера: колонка кнопок и бейдж «сделать» не рисуются, и строка с тегами
        // остаётся единственным содержимым первой ячейки.
        var access = new PlotAccessArguments([], Published: true, ProjectLifecycleStatus.Archived);
        var item = new PlotFolderListItemViewModel(
            new PlotFolderIdentification(ProjectId, 5),
            "Сюжет",
            ElementsCount: 1,
            TodoField: "",
            TagNames: ["лор", "бой"],
            Status: PlotStatus.Completed,
            PlotAccessArguments: access);
        var list = new PlotFolderListViewModel(ProjectId, [item], access, "Сюжеты");

        var cut = Render<PlotFolderListItemRow>(p => p
            .Add(x => x.Item, item)
            .Add(x => x.List, list)
            .Add(x => x.MoveItems, [item]));

        var cell = cut.Find("td").InnerHtml;
        cell.ShouldContain(">#лор</a> <a");
        cell.ShouldContain(">#бой</a> ");
    }

    private sealed class FakeFolderLocator : IUriLocator<PlotFolderIdentification>
    {
        public Uri GetUri(PlotFolderIdentification target) => new($"https://example.org/plot/{target.PlotFolderId}");
    }
}
