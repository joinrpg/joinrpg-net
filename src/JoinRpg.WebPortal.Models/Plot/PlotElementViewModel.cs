using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.Access;
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
    public PlotElementIdentification PlotElementId { get; }

    public bool HasMasterAccess { get; }

    public bool HasEditAccess { get; }

    public bool First { get; set; }
    public bool Last { get; set; }

    public IReadOnlyCollection<GameObjectLinkViewModel> TargetsForDisplay { get; }

    public PlotElementViewModel(Character? character,
        ICurrentUserAccessor currentUser,
        ILinkRenderer linkRendrer,
        PlotTextDto plotElementVersion,
        IUriService uriService,
        ProjectInfo projectInfo)
    {
        ArgumentNullException.ThrowIfNull(linkRendrer);

        ArgumentNullException.ThrowIfNull(plotElementVersion);

        Content = plotElementVersion.Text.Content.ToHtmlString(linkRendrer);
        TodoField = plotElementVersion.Text.TodoField;
        HasMasterAccess = projectInfo.HasMasterAccess(currentUser);
        HasEditAccess = projectInfo.HasMasterAccess(currentUser) && projectInfo.ProjectStatus != ProjectLifecycleStatus.Archived;
        PlotElementId = plotElementVersion.Id.PlotElementId;
        Status = plotElementVersion.GetStatus();
        TargetsForDisplay = [.. plotElementVersion.Target.AllLinks.AsObjectLinks(uriService)];
        CharacterId = character?.CharacterId;
        PublishMode = !HasMasterAccess && !(character?.HasPlayerAccess(currentUser.UserIdOrDefault) ?? false);
    }

    public bool PublishMode { get; }

    int IMovableListItem.ItemId => PlotElementId.PlotElementId;
    public int? CharacterId { get; }

    public bool HasWorkTodo => !string.IsNullOrWhiteSpace(TodoField) || Status == PlotStatus.InWork || Status == PlotStatus.HasNewVersion;


    int IMovableListItem.ProjectId => PlotElementId.ProjectId;
}


public class PlotDisplayViewModel
{
    public PlotDisplayViewModel(IReadOnlyCollection<PlotTextDto> plots,
        ICurrentUserAccessor currentUser,
        Character character,
        IUriService uriService,
        ProjectInfo projectInfo
        )
    {
        ArgumentNullException.ThrowIfNull(plots);

        var accessArguments = AccessArgumentsFactory.Create(character, currentUser.UserIdOrDefault);

        if (plots.Count > 0 && accessArguments.CharacterPlotAccess)
        {
            var linkRenderer = new JoinrpgMarkdownLinkRenderer(character.Project, projectInfo);

            Elements = plots
                .Select(
                    p => new PlotElementViewModel(character,
                        currentUser,
                        linkRenderer,
                        p,
                        uriService,
                        projectInfo
                        ))
                .MarkFirstAndLast();
        }
        else
        {
            Elements = [];
        }
    }

    public IList<PlotElementViewModel> Elements { get; }
}
