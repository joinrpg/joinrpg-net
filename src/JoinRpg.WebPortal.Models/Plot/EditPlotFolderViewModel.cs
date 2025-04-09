using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Markdown;
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
    public IOrderedEnumerable<PlotElementListItemViewModel> Elements { get; private set; }

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
    public void Fill(PlotFolder folder, int? currentUserId, IUriService uriService, ProjectInfo projectInfo)
    {
        PlotFolderMasterTitle = folder.MasterTitle;
        Status = folder.GetStatus();
        Elements = folder.Elements.Select(e => new PlotElementListItemViewModel(e, currentUserId, uriService, projectInfo)).OrderBy(e => e.Status);
        TagNames = folder.PlotTags.Select(tag => tag.TagName).OrderBy(tag => tag).ToList();
        HasEditAccess = folder.HasMasterAccess(currentUserId, acl => acl.CanManagePlots) && folder.Project.Active;
        HasMasterAccess = folder.HasMasterAccess(currentUserId);
    }

    public EditPlotFolderViewModel() { } //For binding

    [ReadOnly(true)]
    public bool HasMasterAccess { get; private set; }
}

public class EditPlotElementViewModel : IProjectIdAware
{

    public EditPlotElementViewModel(PlotElement e, bool hasManageAccess, IUriService uriService)
    {
        PlotElementId = e.PlotElementId;
        Targets = e.GetElementBindingsForEdit();
        Content = e.LastVersion().Content.Contents ?? "";
        TodoField = e.LastVersion().TodoField;
        ProjectId = e.PlotFolder.ProjectId;
        PlotFolderId = e.PlotFolderId;
        Status = e.GetStatus();
        ElementType = (PlotElementTypeView)e.ElementType;
        HasManageAccess = hasManageAccess;
        HasPublishedVersion = e.Published != null;
        TargetsForDisplay = e.GetTargetLinks().AsObjectLinks(uriService).ToList();
    }

    [ReadOnly(true)]
    public int ProjectId { get; }
    [ReadOnly(true)]
    public int PlotFolderId { get; }
    [ReadOnly(true)]
    public int PlotElementId { get; }

    [Display(Name = "Для кого")]
    public IEnumerable<string> Targets { get; set; }
    [Display(Name = "Текст вводной"), UIHint("MarkdownString")]
    public string Content { get; set; }

    [Display(Name = "TODO (что доделать для мастеров)"), DataType(DataType.MultilineText)]
    public string TodoField { get; set; }

    [ReadOnly(true), Display(Name = "Статус")]
    public PlotStatus Status { get; }

    public PlotElementTypeView ElementType { get; }
    public bool HasManageAccess { get; }
    public bool HasPublishedVersion { get; }
    public IEnumerable<GameObjectLinkViewModel> TargetsForDisplay { get; }
}

public class PlotElementListItemViewModel : IProjectIdAware
{

    public PlotElementListItemViewModel(PlotElement e, int? currentUserId, IUriService uriService, ProjectInfo projectInfo, int? currentVersion = null, bool printMode = false)
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
        TargetsForDisplay = e.GetTargetLinks().AsObjectLinks(uriService).ToList();
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
    public IEnumerable<GameObjectLinkViewModel> TargetsForDisplay { get; }

    public PlotElementTypeView ElementType { get; }
    public bool HasEditAccess { get; }

    public bool HasMasterAccess { get; }
    public int CurrentVersion { get; }

    public int? PublishedVersion { get; }
    public string PlotFolderMasterTitle { get; }

    public bool PrintMode { get; }

    public bool ThisPublished => CurrentVersion == PublishedVersion;
}
