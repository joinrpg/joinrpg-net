using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes.Plots;

namespace JoinRpg.Data.Interfaces;

public interface IPlotRepository : IDisposable
{

    Task<List<PlotFolder>> GetPlots(int project);
    Task<List<PlotFolder>> GetPlotsWithTargets(int project);
    Task<PlotFolder?> GetPlotFolderAsync(PlotFolderIdentification plotFolderId);
    Task<IReadOnlyCollection<PlotElement>> GetPlotsForCharacter(Character character);
    Task<IReadOnlyCollection<PlotFolder>> GetPlotsWithTargetAndText(int projectid);
    Task<IReadOnlyCollection<PlotElement>> GetActiveHandouts(int projectid);

    Task<List<PlotFolder>> GetPlotsForTargets(int projectId, List<int> characterIds, List<int> characterGroupIds);

    Task<IReadOnlyCollection<PlotFolder>> GetPlotsByTag(int projectid, string tagname);
}
