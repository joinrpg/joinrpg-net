using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.PrimitiveTypes.Plots;
using JoinRpg.Web.Plots;

namespace JoinRpg.Web.Models.Plot;

public static class PlotTextDtoBuilder
{
    public static PlotTextDto GetDtoForLast(this PlotElement element)
    {
        var version = element.LastVersion();
        return new PlotTextDto()
        {
            Completed = element.GetStatus() == PlotStatus.Completed,
            HasPublished = element.Published != null,
            Latest = true,
            Published = element.Published == version.Version,
            Content = version.Content,
            TodoField = version.TodoField,
            Id = new PlotVersionIdentification(element.ProjectId, element.PlotFolderId, element.PlotElementId, version.Version),
            IsActive = element.IsActive,
            Target = element.ToTarget(),
        };
    }
}
