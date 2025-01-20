using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes.Plots;

namespace JoinRpg.Services.Interfaces;

public interface IPlotService
{
    Task CreatePlotFolder(int projectId, string masterTitle, string todo);
    Task EditPlotFolder(int projectId, int plotFolderId, string plotFolderMasterTitle, string todoField);

    Task<PlotVersionIdentification> CreatePlotElement(PlotFolderIdentification plotFolderId, string content, string todoField,
      IReadOnlyCollection<int> targetGroups, IReadOnlyCollection<int> targetChars, PlotElementType elementType);

    Task DeleteFolder(int projectId, int plotFolderId);
    Task DeleteElement(PlotElementIdentification plotElementId);

    Task EditPlotElement(PlotElementIdentification plotelementid, string contents, string todoField,
      IReadOnlyCollection<int> targetGroups, IReadOnlyCollection<int> targetChars);

    Task MoveElement(int projectId, int plotElementId, int parentCharacterId, int direction);

    Task PublishElementVersion(PlotVersionIdentification version, bool sendNotification, string? commentText);

    Task EditPlotElementText(PlotElementIdentification plotelementid, string content, string todoField);
}
