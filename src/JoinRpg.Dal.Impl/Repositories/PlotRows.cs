using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.Data.Interfaces.Plots;
using JoinRpg.DomainTypes.Plots;
using JoinRpg.Helpers;

namespace JoinRpg.Dal.Impl.Repositories;

/// <summary>
/// Строка выборки по вводной: ровно те поля, которые нужны <see cref="PlotElementDetailsDto"/>.
/// </summary>
/// <remarks>
/// Тип нужен, чтобы выбрать из базы только отображаемую версию текста и метаданные соседних,
/// а не всю историю правок: у крупных папок она составляет основной объём данных. Через
/// EF-сущность так не получается — <c>Include</c> в EF6 фильтровать нельзя, а догрузка версий
/// отдельным запросом заставляла бы вручную помечать коллекцию загруженной, иначе EF при первом
/// обращении лениво добирает все версии.
///
/// Свойства с сеттерами, а не позиционный record: EF6 не умеет вызывать конструкторы в проекции.
/// </remarks>
internal sealed class PlotElementRow : IOrderableEntity
{
    public int ProjectId { get; set; }
    public int PlotFolderId { get; set; }
    public int PlotElementId { get; set; }
    public string PlotFolderMasterTitle { get; set; } = "";

    public PlotElementType ElementType { get; set; }
    public bool IsMasterOnly { get; set; }
    public bool IsActive { get; set; }
    public bool IsCompleted { get; set; }
    public int? PublishedVersion { get; set; }

    public int LastVersionNumber { get; set; }
    public string? LastVersionTodoField { get; set; }

    public int CurrentVersionNumber { get; set; }

    /// <summary>
    /// Есть ли у вводной запрошенная версия. Отдельный флаг, потому что пустой текст существующей
    /// версии от отсутствующей версии по <see cref="CurrentContent"/> не отличить.
    /// </summary>
    public bool CurrentVersionExists { get; set; }

    public string? CurrentContent { get; set; }
    public string? CurrentTodoField { get; set; }
    public DateTime CurrentModifiedAt { get; set; }

    public DateTime? PrevVersionModifiedAt { get; set; }
    public DateTime? NextVersionModifiedAt { get; set; }

    public User? Author { get; set; }

    public IEnumerable<PlotTargetRow> Characters { get; set; } = [];
    public IEnumerable<PlotTargetRow> Groups { get; set; } = [];

    int IOrderableEntity.Id => PlotElementId;

    public PlotElementDetailsDto ToDto()
    {
        var projectId = new ProjectIdentification(ProjectId);

        return new PlotElementDetailsDto(
            new PlotElementIdentification(ProjectId, PlotFolderId, PlotElementId),
            ElementType,
            IsMasterOnly,
            IsActive,
            IsCompleted,
            PublishedVersion,
            new TargetsInfo(
                [.. Characters.Select(c => new CharacterTarget(new CharacterIdentification(projectId, c.Id), c.Name))],
                [.. Groups.Select(g => new GroupTarget(new CharacterGroupIdentification(projectId, g.Id), g.Name))]),
            PlotFolderMasterTitle,
            new PlotElementVersionDto(
                CurrentVersionNumber,
                CurrentContent is null ? null : new MarkdownString(CurrentContent),
                CurrentTodoField ?? "",
                CurrentModifiedAt,
                Author is null ? null : new UserInfoHeader(new UserIdentification(Author.UserId), Author.ExtractDisplayName())),
            LastVersionNumber,
            LastVersionTodoField ?? "",
            PrevVersionModifiedAt,
            NextVersionModifiedAt);
    }
}

/// <summary>Таргет вводной — персонаж или группа.</summary>
internal sealed class PlotTargetRow
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

/// <summary>
/// Строка выборки по папке сюжета: поля для <see cref="PlotFolderDetailsDto"/> вместе со вводными.
/// </summary>
internal sealed class PlotFolderRow
{
    public int ProjectId { get; set; }
    public int PlotFolderId { get; set; }
    public string MasterTitle { get; set; } = "";
    public string? TodoField { get; set; }
    public string? MasterSummary { get; set; }
    public bool IsActive { get; set; }
    public string? ElementsOrdering { get; set; }

    public IEnumerable<string> Tags { get; set; } = [];
    public IEnumerable<PlotElementRow> Elements { get; set; } = [];

    public PlotFolderDetailsDto ToDto()
        => new(
            new PlotFolderIdentification(ProjectId, PlotFolderId),
            MasterTitle,
            TodoField ?? "",
            MasterSummary is null ? null : new MarkdownString(MasterSummary),
            IsActive,
            [.. Tags.Order()],
            [.. Elements.OrderByStoredOrder(ElementsOrdering).Select(e => e.ToDto())]);
}
