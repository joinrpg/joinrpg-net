using JoinRpg.DataModel;
using JoinRpg.Helpers;
using JoinRpg.PrimitiveTypes.Plots;

namespace JoinRpg.Data.Interfaces;

public interface IPlotRepository : IDisposable
{

    Task<IReadOnlyList<PlotFolder>> GetPlots(ProjectIdentification projectId);
    Task<List<PlotFolder>> GetPlotsWithTargets(int project);
    Task<PlotFolder?> GetPlotFolderAsync(PlotFolderIdentification plotFolderId);
    Task<IReadOnlyCollection<PlotElement>> GetPlotsForCharacter(Character character);
    Task<IReadOnlyCollection<PlotFolder>> GetPlotsWithTargetAndText(int projectid);
    Task<IReadOnlyCollection<PlotElement>> GetActiveHandouts(int projectid);

    Task<List<PlotFolder>> GetPlotsForTargets(int projectId, List<int> characterIds, List<int> characterGroupIds);

    Task<IReadOnlyCollection<PlotFolder>> GetPlotsByTag(int projectid, string tagname);

    Task<IReadOnlyCollection<PlotTextDto>> GetPlotsBySpecification(PlotSpecification plotSpecification);

    Task<IReadOnlyCollection<HandoutDto>> GetHandoutsPlotsBySpecification(PlotSpecification plotSpecification);
}

public record PlotSpecification(TargetsInfo Targets, PlotVersionFilter VersionFilter, PlotElementType PlotElementType);

public enum PlotVersionFilter
{
    PublishedVersion,
    LatestVersion,
}

public class PlotTextDto : IOrderableEntity
{
    public required MarkdownString Content { get; set; }
    public required string TodoField { get; set; }
    public required bool Latest { get; set; }
    public required bool Published { get; set; }

    public required bool HasPublished { get; set; }

    public required bool Completed { get; set; }

    public required bool IsActive { get; set; }

    public required PlotVersionIdentification Id { get; set; }

    public required TargetsInfo Target { get; set; }

    int IOrderableEntity.Id => Id.PlotElementId.PlotElementId;
}

public class HandoutDto
{
    public required MarkdownString Handout { get; set; }
    public required User Master { get; set; }

    public required bool Latest { get; set; }
    public required bool Published { get; set; }

    public required bool HasPublished { get; set; }

    public required bool Completed { get; set; }

    public required PlotElement PlotElement { get; set; }
}
