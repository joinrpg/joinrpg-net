using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.Access;
using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Web.Models.Helpers;
using JoinRpg.Web.Plots;

namespace JoinRpg.Web.Models.Plot;

public class PlotDisplayViewModel
{
    public PlotDisplayViewModel(IReadOnlyCollection<PlotTextDto> plots,
        ICurrentUserAccessor currentUser,
        Character character,
        ProjectInfo projectInfo
        )
    {
        ArgumentNullException.ThrowIfNull(plots);

        var accessArguments = AccessArgumentsFactory.Create(character, currentUser, projectInfo);

        CharacterId = character.GetId();
        ShowEditControls = accessArguments.MasterAccess && accessArguments.EditAllowed;

        if (plots.Count == 0 || !accessArguments.CharacterPlotAccess)
        {
            Elements = [];
            return;
        }

        var linkRenderer = new JoinrpgMarkdownLinkRenderer(character.Project, projectInfo);

        Elements = [.. plots.Select(p => p.Render(linkRenderer, projectInfo, currentUser))];
    }

    public IReadOnlyList<PlotRenderedTextViewModel> Elements { get; }

    public CharacterIdentification CharacterId { get; }

    public bool ShowEditControls { get; }
}
