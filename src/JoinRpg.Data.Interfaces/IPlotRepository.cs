using JoinRpg.Data.Interfaces.Plots;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Plots;
using JoinRpg.Helpers;

namespace JoinRpg.Data.Interfaces;

public interface IPlotRepository : IDisposable
{

    Task<IReadOnlyList<PlotFolder>> GetPlots(ProjectIdentification projectId);

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

    /// <summary>
    /// Одна вводная для страниц её просмотра и редактирования: <c>null</c>, если её нет.
    /// </summary>
    /// <param name="elementId">Идентификатор вводной.</param>
    /// <param name="version">Версия для показа; <c>null</c> — последняя.</param>
    /// <exception cref="ArgumentOutOfRangeException">Такой версии у вводной нет.</exception>
    Task<PlotElementDetailsDto?> GetPlotElementDetails(PlotElementIdentification elementId, int? version = null);
    Task<IReadOnlyCollection<PlotElement>> GetDirectPlotsForCharacter(CharacterIdentification characterId);
    /// <summary>
    /// Все неудалённые папки сюжета проекта со вводными — для плоского списка сюжетов.
    /// </summary>
    /// <remarks>
    /// Удалённые папки (<c>IsActive == false</c>) не возвращаются вовсе; удалённые вводные внутри
    /// живой папки — возвращаются, их отфильтровывает уже вызывающий.
    ///
    /// Вводные внутри каждой папки приходят упорядоченными, как и в
    /// <see cref="GetPlotFolderDetails"/>. Порядок самих папок вызывающий задаёт сам.
    /// </remarks>
    Task<IReadOnlyList<PlotFolderDetailsDto>> GetActivePlotFolders(ProjectIdentification projectId);

    [Obsolete]
    Task<List<PlotFolder>> GetPlotsForTargets(int projectId, List<int> characterIds, List<int> characterGroupIds);

    Task<IReadOnlyCollection<PlotFolder>> GetPlotsByTag(int projectid, string tagname);

    Task<IReadOnlyCollection<PlotTextDto>> GetPlotsBySpecification(PlotSpecification plotSpecification);
}

/// <summary>
/// Какие вводные и какие версии выбирает <see cref="IPlotRepository.GetPlotsBySpecification"/>.
/// </summary>
/// <param name="ElementTypes">Типы вводных; пустой набор не вернёт ничего.</param>
public record PlotSpecification(TargetsInfo Targets, PlotVersionFilter VersionFilter, IReadOnlyCollection<PlotElementType> ElementTypes)
{
    /// <summary>Совместный конструктор для выборки одного типа вводных.</summary>
    public PlotSpecification(TargetsInfo Targets, PlotVersionFilter VersionFilter, PlotElementType ElementType)
        : this(Targets, VersionFilter, [ElementType]) { }
}

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

    /// <summary>Тип вводной. RoleGrid рендерится не из текста, а по ссылке на сетку.</summary>
    public required PlotElementType ElementType { get; set; }

    /// <summary>Ссылка на сетку ролей; заполнена только при <see cref="ElementType"/> == RoleGrid.</summary>
    public required ProjectRolesListIdentification? RolesListId { get; set; }

    int IOrderableEntity.Id => Id.PlotElementId.PlotElementId;
}
