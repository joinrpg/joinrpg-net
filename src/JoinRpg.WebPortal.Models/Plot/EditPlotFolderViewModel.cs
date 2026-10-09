using System.Diagnostics.CodeAnalysis;
using JoinRpg.Common.WebComponents;
using JoinRpg.Data.Interfaces.Plots;
using JoinRpg.Domain;
using JoinRpg.Domain.Access;
using JoinRpg.DomainTypes.Plots;
using JoinRpg.Helpers;
using JoinRpg.Interfaces;
using JoinRpg.Markdown;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Plots;
using JoinRpg.Web.Plots.Elements;
using JoinRpg.Web.Plots.Folders;

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
    public IEnumerable<string> TagNames { get; private set; } = [];


    [Required, Display(Name = "Название сюжета", Description = "Вы можете указать теги прямо в названии. Пример: «Интриги Гэндальфа #мордор #гондор #костромская_область»")]
    public string PlotFolderTitleAndTags { get; set; } = "";

    public EditPlotFolderViewModel(PlotFolderDetailsDto folder, ILinkRenderer linkRenderer, ICurrentUserAccessor currentUser, IUriService uriService, ProjectInfo projectInfo)
    {
        if (folder == null)
        {
            throw new ArgumentNullException(nameof(folder));
        }

        PlotFolderId = folder.Id.PlotFolderId;
        TodoField = folder.TodoField;
        ProjectId = folder.Id.ProjectId.Value;
        Fill(folder, linkRenderer, currentUser, uriService, projectInfo);
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
    public void Fill(PlotFolderDetailsDto folder, ILinkRenderer linkRenderer, ICurrentUserAccessor currentUser, IUriService uriService, ProjectInfo projectInfo)
    {
        PlotFolderMasterTitle = folder.MasterTitle;
        Status = folder.GetStatus();

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
    public static IReadOnlyList<PlotElementListItemViewModel> FromFolder(PlotFolderDetailsDto folder, ICurrentUserAccessor currentUserAccessor, ProjectInfo projectInfo, ILinkRenderer linkRenderer)
    {
        // Порядок задаёт репозиторий; здесь он только превращается в список id для контрола перетаскивания.
        var itemIds = folder.Elements.Select(x => x.Id.ToString()).ToArray();
        var accessArguments = AccessArgumentsFactory.CreatePlot(projectInfo, currentUserAccessor);

        return [.. folder.Elements.Select(e => new PlotElementListItemViewModel(
            e, accessArguments, itemIds, linkRenderer))];
    }

    public PlotElementListItemViewModel(
        PlotElementDetailsDto element,
        PlotAccessArguments accessArguments,
        string[]? itemIdsToParticipateInSort,
        ILinkRenderer renderer,
        bool printMode = false)
    {
        var currentVersionText = element.CurrentVersion;

        CurrentVersion = currentVersionText.Version;
        CurrentVersion2 = new PlotVersionIdentification(element.Id, CurrentVersion);

        PlotElementId = element.Id.PlotElementId;
        PlotElementIdentification = element.Id;
        Target = element.Target;
        Content = currentVersionText.Content.ToHtmlString(renderer);
        TodoField = currentVersionText.TodoField;
        ProjectId = element.Id.ProjectId.Value;
        PlotFolderId = element.Id.PlotFolderId.PlotFolderId;
        Status = element.GetStatus();
        ElementType = (PlotElementTypeView)element.ElementType;
        IsMasterOnly = element.IsMasterOnly;
        ShortContent = currentVersionText.Content.TakeWords(10).WithDefaultStringValue("***").ToPlainTextWithoutHtmlEscape(renderer);

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

    public DateTime ModifiedDateTime { get; }

    public UserLinkViewModel? Author { get; }

    public DateTime? PrevModifiedDateTime { get; }

    public DateTime? NextModifiedDateTime { get; }

    [Display(Name = "TODO (что доделать для мастеров)"), DataType(DataType.MultilineText)]
    public string TodoField { get; }

    [ReadOnly(true), Display(Name = "Статус")]
    public PlotStatus Status { get; }

    [ReadOnly(true)]
    public TargetsInfo Target { get; }

    public PlotElementTypeView ElementType { get; }
    public bool IsMasterOnly { get; }

    public bool HasMasterAccess { get; }
    public bool HasEditAccess { get; }
    public int CurrentVersion { get; }

    public PlotVersionIdentification CurrentVersion2 { get; }

    public int? PublishedVersion { get; }

    public PlotVersionIdentification? PubishedVersion2 { get; }
    public string PlotFolderMasterTitle { get; }

    public bool PrintMode { get; }

    // Используется для упорядочивания
    public string[]? ItemsIds { get; }

    public PlotElementPanelViewModel ToPanel() => new(
        PlotElementIdentification,
        Status,
        ElementType,
        IsMasterOnly,
        ShortContent,
        Content.ToHtmlString(),
        TodoField,
        Target,
        AsUtc(ModifiedDateTime),
        Author,
        PrevModifiedDateTime is { } prev ? AsUtc(prev) : null,
        NextModifiedDateTime is { } next ? AsUtc(next) : null,
        CurrentVersion2,
        PubishedVersion2,
        HasMasterAccess,
        HasEditAccess,
        ItemsIds);

    // Даты версий хранятся в UTC; шаблон EventTime сравнивал их с DateTime.UtcNow.
    private static DateTimeOffset AsUtc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
