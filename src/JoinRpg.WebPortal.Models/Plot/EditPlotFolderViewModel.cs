using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Markdown;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.Plots;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Helpers;
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
    public IEnumerable<string> TagNames { get; private set; }


    [Required, Display(Name = "Название сюжета", Description = "Вы можете указать теги прямо в названии. Пример: #мордор #гондор #костромская_область")]
    public string PlotFolderTitleAndTags { get; set; }

    public EditPlotFolderViewModel(PlotFolder folder, int? currentUserId, IUriService uriService, ProjectInfo projectInfo)
    {
        if (folder == null)
        {
            throw new ArgumentNullException(nameof(folder));
        }

        PlotFolderId = folder.PlotFolderId;
        TodoField = folder.TodoField;
        ProjectId = folder.ProjectId;
        Fill(folder, currentUserId, uriService, projectInfo);
        if (TagNames.Any())
        {
            PlotFolderTitleAndTags = folder.MasterTitle + " " + folder.PlotTags.GetTagString();
        }
        else
        {
            PlotFolderTitleAndTags = folder.MasterTitle;
        }
    }

    [MemberNotNull(nameof(TagNames))]
    [MemberNotNull(nameof(Elements))]
    public void Fill(PlotFolder folder, int? currentUserId, IUriService uriService, ProjectInfo projectInfo)
    {
        PlotFolderMasterTitle = folder.MasterTitle;
        Status = folder.GetStatus();
        var orderedElements = folder.Elements.OrderByStoredOrder(folder.ElementsOrdering).ToArray();

        Elements = [.. orderedElements.Select(e => new PlotElementListItemViewModel(e, currentUserId, uriService, projectInfo, [.. orderedElements.Select(x => x.GetId().ToString())]))];
        TagNames = folder.PlotTags.Select(tag => tag.TagName).OrderBy(tag => tag).ToList();
        HasEditAccess = folder.HasMasterAccess(currentUserId, acl => acl.CanManagePlots) && folder.Project.Active;
        HasMasterAccess = folder.HasMasterAccess(currentUserId);
    }

    public EditPlotFolderViewModel() { } //For binding

    [ReadOnly(true)]
    public bool HasMasterAccess { get; private set; }
}

public class EditPlotElementViewModel(PlotElement e, bool hasManageAccess, int? version) : IProjectIdAware
{
    [ReadOnly(true)]
    public int ProjectId { get; } = e.PlotFolder.ProjectId;
    [ReadOnly(true)]
    public int PlotFolderId { get; } = e.PlotFolderId;
    [ReadOnly(true)]
    public int PlotElementId { get; } = e.PlotElementId;

    [Display(Name = "Для кого")]
    public IEnumerable<string> Targets { get; set; } = e.GetElementBindingsForEdit();
    [Display(Name = "Текст вводной"), UIHint("MarkdownString")]
    public string Content { get; set; } = (version is null ? e.LastVersion() : e.SpecificVersion(version.Value))?.Content.Contents ?? "";

    [Display(Name = "TODO (что доделать для мастеров)"), DataType(DataType.MultilineText)]
    public string TodoField { get; set; } = e.LastVersion().TodoField;

    [ReadOnly(true), Display(Name = "Статус")]
    public PlotStatus Status { get; } = e.GetStatus();

    public PlotElementTypeView ElementType { get; } = (PlotElementTypeView)e.ElementType;
    public bool HasManageAccess { get; } = hasManageAccess;
    public bool HasPublishedVersion { get; } = e.Published != null;
    public TargetsInfo Target { get; } = e.ToTarget();
}

public class PlotElementListItemViewModel : IProjectIdAware
{

    public PlotElementListItemViewModel(
        PlotElement e,
        int? currentUserId,
        IUriService uriService,
        ProjectInfo projectInfo,
        string[]? itemIdsToParticipateInSort,
        int? currentVersion = null, bool printMode = false)
    {
        CurrentVersion = currentVersion ?? e.LastVersion().Version;

        var prevVersionText = e.SpecificVersion(CurrentVersion - 1);
        var currentVersionText = e.SpecificVersion(CurrentVersion);
        var nextVersionText = e.SpecificVersion(CurrentVersion + 1);

        if (currentVersionText == null)
        {
            throw new ArgumentOutOfRangeException(nameof(currentVersion));
        }

        var renderer = new JoinrpgMarkdownLinkRenderer(e.Project, projectInfo);

        PlotElementId = e.PlotElementId;
        PlotElementIdentification = new PlotElementIdentification(projectInfo.ProjectId, e.PlotFolderId, e.PlotElementId);
        Target = e.ToTarget();
        Content = currentVersionText.Content.ToHtmlString(renderer);
        TodoField = currentVersionText.TodoField;
        ProjectId = e.PlotFolder.ProjectId;
        PlotFolderId = e.PlotFolderId;
        Status = e.GetStatus();
        ElementType = (PlotElementTypeView)e.ElementType;
        ShortContent = currentVersionText.Content.TakeWords(10)
            .ToPlainText(renderer).WithDefaultStringValue("***");
        HasEditAccess = e.PlotFolder.HasMasterAccess(currentUserId, acl => acl.CanManagePlots) && e.Project.Active;
        HasMasterAccess = e.PlotFolder.HasMasterAccess(currentUserId);
        ModifiedDateTime = currentVersionText.ModifiedDateTime;
        Author = currentVersionText.AuthorUser;
        PrevModifiedDateTime = prevVersionText?.ModifiedDateTime;
        NextModifiedDateTime = nextVersionText?.ModifiedDateTime;

        PlotFolderMasterTitle = e.PlotFolder.MasterTitle;

        PublishedVersion = e.Published;
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

    public User Author { get; }

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
    public bool HasEditAccess { get; }

    public bool HasMasterAccess { get; }
    public bool ShowMoveControl { get; }
    public int CurrentVersion { get; }

    public int? PublishedVersion { get; }
    public string PlotFolderMasterTitle { get; }

    public bool PrintMode { get; }

    public bool ThisPublished => CurrentVersion == PublishedVersion;

    // Используется для упорядочивания
    public string[]? ItemsIds { get; }
}
