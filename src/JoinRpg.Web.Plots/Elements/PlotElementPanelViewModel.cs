namespace JoinRpg.Web.Plots.Elements;

/// <summary>Вводная в списке сюжета: заголовок и текст версии, которую показываем.</summary>
/// <param name="Content">Готовый HTML текста вводной.</param>
/// <param name="SortItemIds">Порядок вводных сюжета для стрелок перемещения; null — стрелок нет.</param>
public record PlotElementPanelViewModel(
    PlotElementIdentification ElementId,
    PlotStatus Status,
    PlotElementTypeView ElementType,
    bool IsMasterOnly,
    string ShortContent,
    string Content,
    string? Todo,
    TargetsInfo Target,
    DateTimeOffset ModifiedAt,
    UserLinkViewModel? Author,
    DateTimeOffset? PrevModifiedAt,
    DateTimeOffset? NextModifiedAt,
    PlotVersionIdentification CurrentVersion,
    PlotVersionIdentification? PublishedVersion,
    bool HasMasterAccess,
    bool HasEditAccess,
    string[]? SortItemIds)
{
    public bool ThisPublished => PublishedVersion?.Version == CurrentVersion.Version;

    /// <summary>Игрокам видны только готовые вводные.</summary>
    public bool Visible => HasMasterAccess || Status == PlotStatus.Completed;

    public PlotVersionIdentification Version(int version) => CurrentVersion with { Version = version };
}
