using System.Diagnostics.CodeAnalysis;
using JoinRpg.Common.WebComponents;
using JoinRpg.Data.Interfaces.Plots;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.Access;
using JoinRpg.DomainTypes.Plots;
using JoinRpg.Helpers;
using JoinRpg.Interfaces;
using JoinRpg.Markdown;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models.Helpers;
using JoinRpg.Web.Plots;

namespace JoinRpg.Web.Models.Plot;

public class EditPlotFolderViewModel : PlotFolderViewModelBase
{
    public int PlotFolderId { get; set; }

    [ReadOnly(true)]
    public IReadOnlyList<PlotElementListItemViewModel> Elements { get; private set; }

    [ReadOnly(true)]
    public bool HasEditAccess { get; private set; }

    [ReadOnly(true)]
    public bool HasPlotEditorAccess { get; private set; }

    [ReadOnly(true)]
    public IEnumerable<string> TagNames { get; private set; }


    [Required, Display(Name = "Название сюжета", Description = "Вы можете указать теги прямо в названии. Пример: «Интриги Гэндальфа #мордор #гондор #костромская_область»")]
    public string PlotFolderTitleAndTags { get; set; }

    public EditPlotFolderViewModel(PlotFolderDetailsDto folder, Project projectForRendering, ICurrentUserAccessor currentUser, IUriService uriService, ProjectInfo projectInfo)
    {
        if (folder == null)
        {
            throw new ArgumentNullException(nameof(folder));
        }

        PlotFolderId = folder.Id.PlotFolderId;
        TodoField = folder.TodoField;
        ProjectId = folder.Id.ProjectId.Value;
        Fill(folder, projectForRendering, currentUser, uriService, projectInfo);
        if (TagNames.Any())
        {
            PlotFolderTitleAndTags = folder.MasterTitle + " " + folder.Tags.Select(t => "#" + t).JoinStrings(" ");
        }
        else
        {
            PlotFolderTitleAndTags = folder.MasterTitle;
        }
    }

    [MemberNotNull(nameof(TagNames))]
    [MemberNotNull(nameof(Elements))]
    public void Fill(PlotFolderDetailsDto folder, Project projectForRendering, ICurrentUserAccessor currentUser, IUriService uriService, ProjectInfo projectInfo)
    {
        PlotFolderMasterTitle = folder.MasterTitle;
        Status = folder.GetStatus();

        var linkRenderer = new JoinrpgMarkdownLinkRenderer(projectForRendering, projectInfo);
        Elements = PlotElementListItemViewModel.FromFolder(folder, currentUser, projectInfo, linkRenderer);
        TagNames = folder.Tags;

        HasEditAccess = projectInfo.HasMasterAccess(currentUser) && projectInfo.IsActive;
        HasPlotEditorAccess = projectInfo.HasMasterAccess(currentUser, Permission.CanManagePlots) && projectInfo.IsActive;
        HasMasterAccess = projectInfo.HasMasterAccess(currentUser);
    }

    public EditPlotFolderViewModel() { } //For binding

    [ReadOnly(true)]
    public bool HasMasterAccess { get; private set; }
}


public class PlotElementListItemViewModel : IProjectIdAware
{
    public static IReadOnlyList<PlotElementListItemViewModel> FromFolder(PlotFolderDetailsDto folder, ICurrentUserAccessor currentUserAccessor, ProjectInfo projectInfo, JoinrpgMarkdownLinkRenderer linkRenderer)
    {
        // Порядок задаёт репозиторий; здесь он только превращается в список id для контрола перетаскивания.
        var itemIds = folder.Elements.Select(x => x.Id.ToString()).ToArray();
        var accessArguments = AccessArgumentsFactory.CreatePlot(projectInfo, currentUserAccessor);

        return [.. folder.Elements.Select(e => new PlotElementListItemViewModel(
            e, accessArguments, itemIds, linkRenderer))];
    }

    /// <summary>Перегрузка для страниц, которые пока держат EF-сущность папки: список сюжетов (FlatList).</summary>
    public static IReadOnlyList<PlotElementListItemViewModel> FromFolder(PlotFolder folder, ICurrentUserAccessor currentUserAccessor, ProjectInfo projectInfo, JoinrpgMarkdownLinkRenderer linkRenderer)
    {
        var orderedElements = folder.Elements.OrderByStoredOrder(folder.ElementsOrdering).ToArray();
        var itemIds = orderedElements.Select(x => x.GetId().ToString()).ToArray();
        var accessArguments = AccessArgumentsFactory.CreatePlot(projectInfo, currentUserAccessor);

        return [.. orderedElements.Select(e => new PlotElementListItemViewModel(
            e.GetDetails(), accessArguments, itemIds, linkRenderer))];
    }

    public PlotElementListItemViewModel(
        PlotElementDetailsDto element,
        PlotAccessArguments accessArguments,
        string[]? itemIdsToParticipateInSort,
        JoinrpgMarkdownLinkRenderer renderer,
        bool printMode = false)
    {
        var currentVersionText = element.CurrentVersion;

        CurrentVersion = currentVersionText.Version;
        CurrentVersion2 = new PlotVersionIdentification(element.Id, CurrentVersion);

        PlotElementId = element.Id.PlotElementId;
        PlotElementIdentification = element.Id;
        Target = element.Target;
        Content = ((MarkdownString?)currentVersionText.Content).ToHtmlString(renderer);
        TodoField = currentVersionText.TodoField;
        ProjectId = element.Id.ProjectId.Value;
        PlotFolderId = element.Id.PlotFolderId.PlotFolderId;
        Status = element.GetStatus();
        ElementType = (PlotElementTypeView)element.ElementType;
        IsMasterOnly = element.IsMasterOnly;
        ShortContent = ((MarkdownString?)currentVersionText.Content).TakeWords(10).WithDefaultStringValue("***").ToPlainTextWithoutHtmlEscape(renderer);

        HasPlotEditorAccess = accessArguments.HasPlotEditorAccess;
        HasMasterAccess = accessArguments.HasMasterAccess;
        HasEditAccess = accessArguments.HasEditAccess;

        ModifiedDateTime = currentVersionText.ModifiedAt;
        Author = currentVersionText.Author is null ? null : new UserLinkViewModel(currentVersionText.Author);
        PrevModifiedDateTime = element.PrevVersionModifiedAt;
        NextModifiedDateTime = element.NextVersionModifiedAt;

        PlotFolderMasterTitle = element.PlotFolderMasterTitle;

        PublishedVersion = element.PublishedVersion;
        PubishedVersion2 = element.PublishedVersion is null
            ? null
            : new PlotVersionIdentification(element.Id, element.PublishedVersion.Value);
        PrintMode = printMode;
        ItemsIds = itemIdsToParticipateInSort;
    }

    [ReadOnly(true)]
    public int ProjectId { get; }
    [ReadOnly(true)]
    public int PlotFolderId { get; }
    [ReadOnly(true)]
    public int PlotElementId { get; }

    [ReadOnly(true)]
    public PlotElementIdentification PlotElementIdentification { get; }

    [Display(Name = "Текст вводной"), UIHint("MarkdownString")]
    public JoinHtmlString Content { get; }

    public string ShortContent { get; }

    [UIHint("EventTime")]
    public DateTime ModifiedDateTime { get; }

    public UserLinkViewModel? Author { get; }

    [UIHint("EventTime")]
    public DateTime? PrevModifiedDateTime { get; }

    [UIHint("EventTime")]
    public DateTime? NextModifiedDateTime { get; }

    [Display(Name = "TODO (что доделать для мастеров)"), DataType(DataType.MultilineText)]
    public string TodoField { get; }

    [ReadOnly(true), Display(Name = "Статус")]
    public PlotStatus Status { get; }

    [ReadOnly(true)]
    public TargetsInfo Target { get; }

    public PlotElementTypeView ElementType { get; }
    public bool IsMasterOnly { get; }
    public bool HasPlotEditorAccess { get; }

    public bool HasMasterAccess { get; }
    public bool HasEditAccess { get; }
    public bool ShowMoveControl { get; }
    public int CurrentVersion { get; }

    public PlotVersionIdentification CurrentVersion2 { get; }

    public int? PublishedVersion { get; }

    public PlotVersionIdentification? PubishedVersion2 { get; }
    public string PlotFolderMasterTitle { get; }

    public bool PrintMode { get; }

    public bool ThisPublished => CurrentVersion == PublishedVersion;

    // Используется для упорядочивания
    public string[]? ItemsIds { get; }
}
