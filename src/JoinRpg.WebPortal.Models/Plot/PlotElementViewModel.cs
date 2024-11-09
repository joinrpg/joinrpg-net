using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Helpers.Web;
using JoinRpg.Markdown;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models.CommonTypes;

namespace JoinRpg.Web.Models.Plot;

public class PlotElementViewModel : IMovableListItem
{
    public PlotStatus Status { get; }
    public JoinHtmlString Content { get; }
    public string TodoField { get; }
    public int PlotFolderId { get; }

    public int PlotElementId { get; }
    public int ProjectId { get; }
    public bool HasMasterAccess { get; }

    public bool HasEditAccess { get; }

    public bool First { get; set; }
    public bool Last { get; set; }

    public IEnumerable<GameObjectLinkViewModel> TargetsForDisplay { get; }

    public PlotElementViewModel(Character? character,
        int? currentUserId,
        ILinkRenderer linkRendrer,
        PlotElementTexts plotElementVersion,
        IUriService uriService)
    {
        if (linkRendrer == null)
        {
            throw new ArgumentNullException(nameof(linkRendrer));
        }

        if (plotElementVersion == null)
        {
            throw new ArgumentNullException(nameof(plotElementVersion));
        }

        var p = plotElementVersion.PlotElement;

        Content = plotElementVersion.Content.ToHtmlString(linkRendrer);
        TodoField = plotElementVersion.TodoField;
        HasMasterAccess = p.HasMasterAccess(currentUserId);
        HasEditAccess = p.HasMasterAccess(currentUserId) && p.Project.Active;
        PlotFolderId = p.PlotFolderId;
        PlotElementId = p.PlotElementId;
        ProjectId = p.ProjectId;
        Status = p.GetStatus();
        TargetsForDisplay = p.GetTargetLinks().AsObjectLinks(uriService).ToList();
        CharacterId = character?.CharacterId;
        PublishMode = !HasMasterAccess && !(character?.HasPlayerAccess(currentUserId) ?? false);
    }

    public bool PublishMode { get; }

    int IMovableListItem.ItemId => PlotElementId;
    public int? CharacterId { get; }

    public bool HasWorkTodo => !string.IsNullOrWhiteSpace(TodoField) || Status == PlotStatus.InWork || Status == PlotStatus.HasNewVersion;
}


public class PlotDisplayViewModel
{
    public static PlotDisplayViewModel Published(IReadOnlyCollection<PlotElement> plots,
        int? currentUserId,
        Character character,
        IUriService uriService) =>
        new(plots,
            currentUserId,
            character,
            true,
            PlotElementType.RegularPlot,
            uriService);

    private PlotDisplayViewModel(IReadOnlyCollection<PlotElement> plots,
        int? currentUserId,
        Character? character,
        bool publishedOnly,
        PlotElementType plotElementType,
        IUriService uriService)
    {
        if (plots == null)
        {
            throw new ArgumentNullException(nameof(plots));
        }

        var projectEntity = ((IProjectEntity)character ?? plots.FirstOrDefault())?.Project;
        var hasMasterAccess = projectEntity?.HasMasterAccess(currentUserId) ?? false;

        var hasPlayerAccess = character?.HasPlayerAccess(currentUserId) ?? false;



        if (plots.Any() && projectEntity != null &&
            (hasMasterAccess || hasPlayerAccess || projectEntity.Details.PublishPlot))
        {
            if (!hasMasterAccess && !publishedOnly)
            {
                throw new NoAccessToProjectException(projectEntity, currentUserId);
            }
            var linkRenderer = new JoinrpgMarkdownLinkRenderer(plots.First().Project);

            Func<PlotElement, PlotElementTexts?> selector;
            if (!publishedOnly)
            {
                selector = element => element.LastVersion();
            }
            else
            {
                selector = element => element.PublishedVersion();
            }

            Elements = plots.Where(p => p.ElementType == plotElementType && p.IsActive == true)
                .Select(selector)
                .WhereNotNull()
                .Select(
                    p => new PlotElementViewModel(character,
                        currentUserId,
                        linkRenderer,
                        p,
                        uriService))
                .MarkFirstAndLast();

            HasUnready = plots.Any(element => element.ElementType == plotElementType &&
                                              element.Published !=
                                              element.Texts.Max(text => text.Version));
        }
        else
        {
            Elements = Enumerable.Empty<PlotElementViewModel>();
        }
    }

    private PlotDisplayViewModel() => Elements = Enumerable.Empty<PlotElementViewModel>();

    public IEnumerable<PlotElementViewModel> Elements { get; }
    public bool HasUnready { get; }

    public static PlotDisplayViewModel Empty() => new();
}
