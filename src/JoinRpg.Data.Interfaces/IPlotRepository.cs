using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Plots;
using JoinRpg.Helpers;

namespace JoinRpg.Data.Interfaces;

public interface IPlotRepository : IDisposable
{

    Task<IReadOnlyList<PlotFolder>> GetPlots(ProjectIdentification projectId);
    Task<PlotFolder?> GetPlotFolderAsync(PlotFolderIdentification plotFolderId);

    /// <summary>
    /// Отдаёт EF-граф проекта, который нужен <c>JoinrpgMarkdownLinkRenderer</c>: персонажи, группы,
    /// заявки и мастера. Без него рендеринг текста вводной грузит их лениво, по одному.
    /// </summary>
    /// <remarks>
    /// Метод существует только потому, что рендерер markdown завязан на EF-сущность <c>Project</c>,
    /// и уйдёт вместе с этой завязкой — см. #4923. Звать его нужно там и только там, где текст
    /// вводной действительно рендерится: страницам, которым нужны лишь таргеты или заголовки,
    /// граф проекта не нужен.
    ///
    /// Раньше эту загрузку молча делал <see cref="GetPlotFolderAsync"/> — из-за чего её платили
    /// и те страницы, которым она не нужна.
    /// </remarks>
    Task<Project> GetProjectForPlotRendering(ProjectIdentification projectId);
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
