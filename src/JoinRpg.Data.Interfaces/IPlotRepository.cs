using JetBrains.Annotations;
using JoinRpg.DataModel;

namespace JoinRpg.Data.Interfaces;

public interface IPlotRepository : IDisposable
{

    Task<List<PlotFolder>> GetPlots(int project);
    Task<List<PlotFolder>> GetPlotsWithTargets(int project);
    [ItemCanBeNull]
    Task<PlotFolder> GetPlotFolderAsync(int projectId, int plotFolderId);
    Task<IReadOnlyCollection<PlotElement>> GetPlotsForCharacter([NotNull] Character character);
    Task<IReadOnlyCollection<PlotFolder>> GetPlotsWithTargetAndText(int projectid);
    [ItemNotNull]
    Task<IReadOnlyCollection<PlotElement>> GetActiveHandouts(int projectid);

    [ItemNotNull]
    Task<List<PlotFolder>> GetPlotsForTargets(int projectId, List<int> characterIds, List<int> characterGroupIds);

    Task<IReadOnlyCollection<PlotFolder>> GetPlotsByTag(int projectid, string tagname);
}
