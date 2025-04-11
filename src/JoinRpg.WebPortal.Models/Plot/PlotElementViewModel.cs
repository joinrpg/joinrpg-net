using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.Access;
using JoinRpg.Helpers;
using JoinRpg.Interfaces;
using JoinRpg.Markdown;
using JoinRpg.PrimitiveTypes.Plots;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Plots;

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
        ICurrentUserAccessor currentUser,
        ILinkRenderer linkRendrer,
        PlotElementTexts plotElementVersion,
        IUriService uriService,
        ProjectInfo projectInfo)
    {
        ArgumentNullException.ThrowIfNull(linkRendrer);

        ArgumentNullException.ThrowIfNull(plotElementVersion);

        var p = plotElementVersion.PlotElement;

        Content = plotElementVersion.Content.ToHtmlString(linkRendrer);
        TodoField = plotElementVersion.TodoField;
        HasMasterAccess = projectInfo.HasMasterAccess(currentUser);
        HasEditAccess = projectInfo.HasMasterAccess(currentUser) && p.Project.Active;
        PlotFolderId = p.PlotFolderId;
        PlotElementId = p.PlotElementId;
        ProjectId = p.ProjectId;
        Status = p.GetStatus();
        TargetsForDisplay = p.GetTargetLinks().AsObjectLinks(uriService).ToList();
        CharacterId = character?.CharacterId;
        PublishMode = !HasMasterAccess && !(character?.HasPlayerAccess(currentUser.UserIdOrDefault) ?? false);
    }

    public bool PublishMode { get; }

    int IMovableListItem.ItemId => PlotElementId;
    public int? CharacterId { get; }

    public bool HasWorkTodo => !string.IsNullOrWhiteSpace(TodoField) || Status == PlotStatus.InWork || Status == PlotStatus.HasNewVersion;
}


public class PlotDisplayViewModel
{
    public static PlotDisplayViewModel Published(IReadOnlyCollection<PlotElement> plots,
        ICurrentUserAccessor currentUser,
        Character character,
        IUriService uriService, ProjectInfo projectInfo) =>
        new(plots,
            currentUser,
            character,
            uriService,
            projectInfo);

    private PlotDisplayViewModel(IReadOnlyCollection<PlotElement> plots,
        ICurrentUserAccessor currentUser,
        Character character,
        IUriService uriService,
        ProjectInfo projectInfo)
    {
        ArgumentNullException.ThrowIfNull(plots);

        var accessArguments = AccessArgumentsFactory.Create(character, currentUser.UserIdOrDefault);

        if (plots.Count > 0 && (accessArguments.AnyAccessToCharacter || character.Project.Details.PublishPlot))
        {
            var linkRenderer = new JoinrpgMarkdownLinkRenderer(character.Project, projectInfo);

            Elements = plots.Where(p => p.ElementType == PlotElementType.RegularPlot && p.IsActive == true)
                .Select(element => element.PublishedVersion())
                .WhereNotNull()
                .Select(
                    p => new PlotElementViewModel(character,
                        currentUser,
                        linkRenderer,
                        p,
                        uriService,
                        projectInfo
                        ))
                .MarkFirstAndLast();

            HasUnready = plots.Any(element => element.ElementType == PlotElementType.RegularPlot &&
                                              element.Published !=
                                              element.Texts.Max(text => text.Version));
        }
        else
        {
            Elements = [];
        }
    }

    private PlotDisplayViewModel() => Elements = [];

    public IList<PlotElementViewModel> Elements { get; }
    public bool HasUnready { get; }

    public static PlotDisplayViewModel Empty() => new();
}
