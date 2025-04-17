using JoinRpg.Data.Interfaces;
using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.Markdown;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Web.Plots;
using Microsoft.AspNetCore.Components;

namespace JoinRpg.Web.Models.Plot;

public static class PlotRenderer
{
    public static PlotRenderedTextViewModel Render(this PlotTextDto self, ILinkRenderer linkRenderer, ProjectInfo projectInfo, ICurrentUserAccessor currentUser)
    {
        var masterAccess = projectInfo.HasMasterAccess(currentUser);
        return new PlotRenderedTextViewModel(
            self.Content.ToHtmlString(linkRenderer),
            masterAccess ? new MarkupString(self.TodoField) : null,
            self.Id,
            masterAccess ? self.GetStatus() : null,
            self.Target);
    }
}
