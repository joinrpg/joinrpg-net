using JoinRpg.Data.Interfaces.Plots;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Plots;
using JoinRpg.Helpers;

namespace JoinRpg.Data.Interfaces;

public interface IPlotRepository : IDisposable
{

    Task<IReadOnlyList<PlotFolder>> GetPlots(ProjectIdentification projectId);
    /// <summary>
    /// Папка сюжета как EF-сущность.
    /// </summary>
    /// <remarks>
    /// Осталась у страниц одной вводной (<c>EditElement</c>, <c>ShowElementVersion</c>,
    /// <c>CreateElement</c> с копированием) и у списка сюжетов. Им нужна не папка целиком, а один
    /// элемент — метод уйдёт, когда для этого появится отдельный запрос.
    /// </remarks>
    [Obsolete("Используйте GetPlotFolderDetails; для одной вводной нужен отдельный метод")]
    Task<PlotFolder?> GetPlotFolderAsync(PlotFolderIdentification plotFolderId);

    /// <summary>
    /// Папка сюжета со вводными для страниц просмотра и редактирования: <c>null</c>, если её нет.
    /// </summary>
    /// <remarks>
    /// Вводные приходят упорядоченными, каждая — с последней версией текста и датами соседних версий.
    /// Тексты остальных версий не выбираются: страницам они не нужны, а у больших папок история
    /// правок составляет основной объём данных.
    ///
    /// Если нужен ещё и рендеринг markdown, граф проекта берётся отдельно —
    /// <c>IProjectRepository.GetProjectForMarkdownRendering</c>.
    /// </remarks>
    Task<PlotFolderDetailsDto?> GetPlotFolderDetails(PlotFolderIdentification plotFolderId);
    Task<IReadOnlyCollection<PlotElement>> GetDirectPlotsForCharacter(CharacterIdentification characterId);
    Task<IReadOnlyCollection<PlotFolder>> GetPlotsWithTargetAndText(int projectid);

    [Obsolete]
    Task<List<PlotFolder>> GetPlotsForTargets(int projectId, List<int> characterIds, List<int> characterGroupIds);

    Task<IReadOnlyCollection<PlotFolder>> GetPlotsByTag(int projectid, string tagname);

    Task<IReadOnlyCollection<PlotTextDto>> GetPlotsBySpecification(PlotSpecification plotSpecification);
}

public record PlotSpecification(TargetsInfo Targets, PlotVersionFilter VersionFilter, PlotElementType PlotElementType);

public enum PlotVersionFilter
{
    PublishedVersion,
    LatestVersion,
}

public class PlotTextDto : IOrderableEntity
{
    public required MarkdownDbValue Content { get; set; }
    public required string TodoField { get; set; }
    public required bool Latest { get; set; }
    public required bool Published { get; set; }

    public required bool HasPublished { get; set; }

    public required bool Completed { get; set; }

    public required bool IsActive { get; set; }

    public required PlotVersionIdentification Id { get; set; }

    public required TargetsInfo Target { get; set; }

    int IOrderableEntity.Id => Id.PlotElementId.PlotElementId;
}
