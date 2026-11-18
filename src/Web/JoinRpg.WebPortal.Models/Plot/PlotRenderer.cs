using JoinRpg.Data.Interfaces;
using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.Markdown;
using JoinRpg.Web.Plots.Elements;

namespace JoinRpg.Web.Models.Plot;

public static class PlotRenderer
{
    public static PlotRenderedTextViewModel Render(this PlotTextDto self, ILinkRenderer linkRenderer, ProjectInfo projectInfo, ICurrentUserAccessor currentUser)
    {
        var masterAccess = projectInfo.HasMasterAccess(currentUser);
        var masterOrPublishAccess = masterAccess || projectInfo.PublishPlot;
        var elementType = (PlotElementTypeView)self.ElementType;
        var rolesListName = self.RolesListId is { } rolesListId
            ? projectInfo.GetRolesListByIdOrDefault(rolesListId)?.Name
            : null;
        // У вводной-сетки текста нет: в диалоге перестановки вместо одинакового «***»
        // показываем имя сетки, чтобы несколько сеток можно было различить.
        var shortContent = elementType == PlotElementTypeView.RoleGrid
            ? PlotRenderedTextViewModel.FormatRoleGridCaption(rolesListName)
            : ((MarkdownString?)self.Content).TakeWords(10).WithDefaultStringValue("***").ToPlainTextWithoutHtmlEscape(linkRenderer);

        return new PlotRenderedTextViewModel(
            ((MarkdownString?)self.Content).ToHtmlString(linkRenderer).Value ?? "",
            shortContent,
            masterAccess ? self.TodoField : null,
            self.Id,
            masterOrPublishAccess ? self.GetStatus() : null,
            masterOrPublishAccess ? self.Target : null,
            elementType,
            self.RolesListId,
            rolesListName);
    }
}
